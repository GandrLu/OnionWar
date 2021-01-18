﻿using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerShooting : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Serialized Fields
    [SerializeField] List<GameObject> weaponPrefabs;
    [SerializeField] Transform rig;
    [SerializeField] Transform aimingPlane;
    [SerializeField] Transform aimHold;

    [SerializeField] MultiParentConstraint weaponParentConstraint;
    [SerializeField] MultiAimConstraint headAimConstraint;
    [SerializeField] TwoBoneIKConstraint leftArmTwoBoneConstraint;
    [SerializeField] TwoBoneIKConstraint rightArmTwoBoneConstraint;

    [Tooltip("Defines how fast the movement inaccuracy increases.")]
    [SerializeField] float inaccuracyAcceleration = 2f;
    [Tooltip("Defines how fast the movement inaccuracy decreases.")]
    [SerializeField] float inaccuracyDeceleration = 0.75f;
    [Tooltip("Defines how much the movement speed influences the movement inaccuracy.")]
    [SerializeField] float moveSpeedInaccuracyImpact = 3f;
    [Tooltip("Defines how much the weapons and the movement inaccuracy affect a shot.")]
    [SerializeField] float inaccuracyShootImpact = 5f;
    #endregion

    #region Private Fields
    private Animator anim;
    private AudioSource audioSource;
    private PlayerMovement playerMovement;
    private LineRenderer aimingLine;
    private PersonalWeapon weaponInHands;
    private ParticleSystem muzzleFlashParticles;
    // RaycastHit variable to store information about what was hit by the aiming ray.
    private RaycastHit shootableHit;

    private RigBuilder rigBuilder;
    private MultiParentConstraint leftHandParentConstraint;
    private MultiParentConstraint rightHandParentConstraint;

    private bool isAiming, isReloading, isReadyToFire;
    private int shootableMask;
    private int activeWeaponIndex = 0;
    private float aimingDistance = 30f;
    private float camRayLength = 100f;
    private float currentInaccuracy;
    private float timeToReload = 0.7f;        // The time between each shot.
    private float reloadCooldownTimer, shotCooldownTimer;
    private Vector3 shotPosition;
    private Vector3 aimHoldRotation;

    public int ActiveWeaponIndex { get => activeWeaponIndex; set => activeWeaponIndex = value; }
    #endregion

    #region Photon Callbacks
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isAiming);
            stream.SendNext(aimHoldRotation);
        }
        else
        {
            isAiming = (bool)stream.ReceiveNext();
            if (weaponInHands != null)
                SetRigConstraints(isAiming);
            aimHoldRotation = (Vector3)stream.ReceiveNext();
            if (isAiming)
                aimHold.forward = aimHoldRotation;
        }
    }
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        shootableMask = LayerMask.GetMask(new string[] { "Default", "HitBox" });
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        playerMovement = GetComponent<PlayerMovement>();
        rigBuilder = GetComponent<RigBuilder>();
        leftHandParentConstraint = leftArmTwoBoneConstraint.GetComponentInChildren<MultiParentConstraint>();
        rightHandParentConstraint = rightArmTwoBoneConstraint.GetComponentInChildren<MultiParentConstraint>();
    }

    private void Start()
    {
        // Initially equip weapon if it was not equipped over network and weapons are in the prefab list
        if (weaponPrefabs.Count <= 0)
            return;
        if (weaponInHands == null)
            ChangeWeapon(ActiveWeaponIndex);
        // Use in start to ensure weapon is loaded
        if (GameManager.Instance != null)
            GameManager.Instance.AmmoText.text = weaponInHands.LoadedBullets + " / " + weaponInHands.BulletChamberSize;
    }

    private void Update()
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true
            || weaponInHands == null)
        {
            return;
        }

        // Timers
        if (!isReadyToFire && !isReloading)
            ShotCooldown();
        if (isReloading)
            ReloadCooldown();

        if (Input.GetButtonDown("WeaponSlot1") && weaponPrefabs.Count > 0 && ActiveWeaponIndex != 0)
        {
            ChangeWeapon(0);
            photonView.RPC(nameof(ChangeWeapon), RpcTarget.Others, 0);
        }
        if (Input.GetButtonDown("WeaponSlot2") && weaponPrefabs.Count > 1 && ActiveWeaponIndex != 1)
        {
            ChangeWeapon(1);
            photonView.RPC(nameof(ChangeWeapon), RpcTarget.Others, 1);
        }

        // Inaccuracy of shooting
        HandleInaccuracy();

        // Reloading
        if (weaponInHands.LoadedBullets <= 0 && !isReloading)
            StartReload();

        // Aiming
        isAiming = Input.GetButton("Fire2");
        aimingLine.enabled = isAiming && isReadyToFire && !isReloading;

        if (isAiming)
        {
            shotPosition = weaponInHands.ShootingPosition.position;
            Aim();
            SetRigConstraints(true);
        }
        else
            SetRigConstraints(false);


        // Shooting
        if (Input.GetButtonDown("Fire1") && isAiming && isReadyToFire && !isReloading)
            Shoot();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (aimingLine != null)
            aimingLine.enabled = false;
    }
    #endregion

    #region Public Methods
    public void ActivateWeaponInHands(bool isToActivate)
    {
        if (weaponInHands != null)
            weaponInHands.gameObject.SetActive(isToActivate);
    }

    public void ReloadWeapon()
    {
        if (weaponInHands == null)
            return;
        isReloading = false;
        weaponInHands.LoadedBullets = weaponInHands.BulletChamberSize;
        GameManager.Instance.AmmoText.text = weaponInHands.LoadedBullets + " / " + weaponInHands.BulletChamberSize;
        reloadCooldownTimer = 0;
        shotCooldownTimer = 0;
    }
    #endregion

    #region PunRPCs
    [PunRPC]
    public void ChangeWeapon(int weaponIndex)
    {
        if (weaponIndex >= weaponPrefabs.Count || weaponIndex < 0)
            return;
        if (weaponInHands != null)
        {
            Destroy(weaponInHands.gameObject, 0.1f);
            weaponInHands.gameObject.SetActive(false);
        }
        GameObject weaponObj = Instantiate(weaponPrefabs[weaponIndex], rig);
        weaponInHands = weaponObj.GetComponent<PersonalWeapon>();
        SetupWeaponRigConstraint();

        muzzleFlashParticles = weaponInHands.MuzzleFlash;
        aimingLine = weaponInHands.AimingLine;
        aimingPlane.localPosition = new Vector3(0, aimHold.transform.position.y, 0);
        ActiveWeaponIndex = weaponIndex;

        if (weaponInHands.WeaponType == PersonalWeaponType.Rifle)
            anim.SetBool("hold" + PersonalWeaponType.Rifle.ToString(), true);
        else if (weaponInHands.WeaponType == PersonalWeaponType.Pistol)
            anim.SetBool("hold" + PersonalWeaponType.Pistol, true);
    }

    [PunRPC]
    public void PlayShotEffects()
    {
        muzzleFlashParticles.Play();
        audioSource.PlayOneShot(audioSource.clip);
    }
    #endregion

    #region Private Methods
    private void HandleInaccuracy()
    {
        var movementInaccuracy = playerMovement.Speed * moveSpeedInaccuracyImpact;

        // Inaccuracy is increasing
        if (movementInaccuracy > currentInaccuracy)
            currentInaccuracy =
                Mathf.Lerp(currentInaccuracy, movementInaccuracy, inaccuracyAcceleration * Time.deltaTime);
        // Inaccuracy is decreasing
        else
            currentInaccuracy =
                Mathf.Lerp(currentInaccuracy, movementInaccuracy, inaccuracyDeceleration * Time.deltaTime);

        if (currentInaccuracy < 0.00001f)
            currentInaccuracy = 0f;
        CursorManager.Instance.ResizeCrosshair(currentInaccuracy + weaponInHands.Accuracy);
    }

    private void ShotCooldown()
    {
        shotCooldownTimer -= Time.deltaTime;
        if (shotCooldownTimer < 0)
        {
            isReadyToFire = true;
            shotCooldownTimer = 0;
        }
    }

    private void ResetShotCooldown()
    {
        shotCooldownTimer += weaponInHands.TimeBetweenShots;
        isReadyToFire = false;
    }

    private void SetRigConstraints(bool shouldSetToAim)
    {
        var sources = weaponParentConstraint.data.sourceObjects;
        if (shouldSetToAim)
        {
            // Weapon
            sources.SetWeight(0, 1f);
            sources.SetWeight(2, 0f);
            sources.SetWeight(3, 0f);
            // Arms
            leftArmTwoBoneConstraint.weight = 1f;
            rightArmTwoBoneConstraint.weight = 1f;
            // Head
            headAimConstraint.weight = 0.7f;
        }
        else
        {
            // Weapon
            sources.SetWeight(0, 0f);
            if (weaponInHands.WeaponType == PersonalWeaponType.Rifle)
            {
                sources.SetWeight(2, 1f);
                sources.SetWeight(3, 0f);
            }
            else if (weaponInHands.WeaponType == PersonalWeaponType.Pistol)
            {
                sources.SetWeight(2, 0f);
                sources.SetWeight(3, 1f);
            }
            // Arms
            leftArmTwoBoneConstraint.weight = 0f;
            rightArmTwoBoneConstraint.weight = 0f;
            // Head
            headAimConstraint.weight = 0f;
        }
        weaponParentConstraint.data.sourceObjects = sources;
    }

    private void StartReload()
    {
        reloadCooldownTimer += weaponInHands.ReloadTime;
        GameManager.Instance.AmmoText.text = "Reload";
        isReadyToFire = false;
        isReloading = true;
    }

    private void ReloadCooldown()
    {
        reloadCooldownTimer -= Time.deltaTime;
        if (reloadCooldownTimer < 0)
            ReloadWeapon();
    }

    private void Shoot()
    {
        // Inaccuracy
        var combinedInaccuracy = weaponInHands.Accuracy + currentInaccuracy;
        var x = inaccuracyShootImpact *
            Random.Range(-combinedInaccuracy, combinedInaccuracy);
        var y = inaccuracyShootImpact *
            Random.Range(-combinedInaccuracy, combinedInaccuracy);

        ResetShotCooldown();
        photonView.RPC(nameof(PlayShotEffects), RpcTarget.All);
        ReduceBullets();
        currentInaccuracy += weaponInHands.Recoil;

        if (shootableHit.collider == null)
        {
            var shotDirection = transform.forward + new Vector3(x, y, 0);
            //Debug.DrawRay(shotPosition, shotDirection * 20, Color.blue, 60);
            Ray shotRay = new Ray(shotPosition, shotDirection);
            PhotonNetwork.Instantiate("ProjectileTrail", shotPosition, Quaternion.LookRotation(shotDirection));

            // Perform the raycast and if it hits something on the shootable layer...
            if (Physics.Raycast(shotRay, out shootableHit, camRayLength, shootableMask))
            {
                var hitBox = shootableHit.collider.GetComponent<HitBox>();
                if (hitBox != null)
                    hitBox.Hit(shootableHit.point, Quaternion.Euler(-shotDirection));
            }
        }
        else
        {
            var shotDirection = shootableHit.point - shotPosition + new Vector3(x, y, 0);
            //Debug.DrawRay(shotPosition, shotDirection * 10, Color.cyan, 60);
            Ray shotRay = new Ray(shotPosition, shotDirection);
            PhotonNetwork.Instantiate("ProjectileTrail", shotPosition, Quaternion.LookRotation(shotDirection));

            RaycastHit directShotHit;
            if (Physics.Raycast(shotRay, out directShotHit, camRayLength, shootableMask))
            {
                var hitBox = directShotHit.collider.GetComponent<HitBox>();
                if (hitBox != null)
                    hitBox.Hit(directShotHit.point, Quaternion.FromToRotation(Vector3.forward, shotDirection));
            }
        }
    }

    private void Aim()
    {
        aimingLine.SetPosition(0, shotPosition);
        // Create a ray from the mouse cursor on screen in the direction of the camera.
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 shotTarget = new Vector3();
        // Perform the raycast and if it hits something on the shootable layer...
        if (Physics.Raycast(camRay, out shootableHit, camRayLength, shootableMask))
        {
            Vector3 aimingVector = shootableHit.point - shotPosition;
            //Debug.Log("Pos " + playerToMouse);
            //Debug.Log("ScreenRay " + shootableHit.point);
            aimingLine.SetPosition(1, shootableHit.point);
            shotTarget = shootableHit.point;
        }
        else
        {
            aimingLine.SetPosition(1, shotPosition + transform.forward * aimingDistance);
            shotTarget = shotPosition + transform.forward * aimingDistance;
        }
        aimHold.forward = shotTarget - shotPosition;
        aimHoldRotation = shotTarget - shotPosition;
    }

    private void ReduceBullets()
    {
        --weaponInHands.LoadedBullets;
        GameManager.Instance.AmmoText.text = weaponInHands.LoadedBullets + " / " + weaponInHands.BulletChamberSize;
    }

    private void SetupWeaponRigConstraint()
    {
        weaponParentConstraint.data.constrainedObject = weaponInHands.transform;
        var sourceL = new WeightedTransformArray(0);
        var sourceR = new WeightedTransformArray(0);
        sourceL.Add(new WeightedTransform(weaponInHands.RefLeft, 1f));
        sourceR.Add(new WeightedTransform(weaponInHands.RefRight, 1f));
        leftHandParentConstraint.data.sourceObjects = sourceL;
        rightHandParentConstraint.data.sourceObjects = sourceR;
        rigBuilder.Build();
        anim.Rebind();
    }
    #endregion
}
