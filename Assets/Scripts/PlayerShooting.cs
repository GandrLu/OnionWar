using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerShooting : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Serialized Fields
    [SerializeField] GameObject weaponPrefab;
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
    private float aimingDistance = 30f;
    private float camRayLength = 100f;
    private float currentInaccuracy;
    private float timeToReload = 0.7f;        // The time between each shot.
    private float reloadCooldownTimer, shotCooldownTimer;
    private Vector3 shotPosition;
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
        // Initially equip weapon
        ChangeWeapon(weaponPrefab);
        // Use in start to ensure weapon is loaded
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

        // Inaccuracy of shooting
        HandleInaccuracy();

        // Reloading
        if (weaponInHands.LoadedBullets <= 0 && !isReloading)
            StartReload();

        // Aiming
        isAiming = Input.GetButton("Fire2");
        aimingLine.enabled = isAiming && isReadyToFire && !isReloading;
        SetAimingAnimation(isAiming);

        if (isAiming)
        {
            shotPosition = weaponInHands.ShootingPosition.position;
            Aim();
            photonView.RPC(nameof(SetWeaponParentConstraintAim), RpcTarget.All);
        }
        else
            photonView.RPC(nameof(SetWeaponParentConstraintHold), RpcTarget.All);
        
        SetArmAndHeadConstraint(isAiming);

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
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isAiming);
        }
        else
        {
            bool aiming = (bool)stream.ReceiveNext();
            SetAimingAnimation(aiming);
        }
    }

    public void ChangeWeapon(GameObject weapon)
    {
        GameObject weaponObj = Instantiate(weapon, rig);
        weaponInHands = weaponObj.GetComponent<PersonalWeapon>();
        SetupWeaponRigConstraint();
        
        muzzleFlashParticles = weaponInHands.MuzzleFlash;
        aimingLine = weaponInHands.AimingLine;
        aimingPlane.localPosition = new Vector3(0, weaponInHands.transform.position.y, 0);
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
    public void PlayShotEffects()
    {
        muzzleFlashParticles.Play();
        audioSource.PlayOneShot(audioSource.clip);
    }

    [PunRPC]
    public void SetWeaponParentConstraintAim()
    {
        var sources = weaponParentConstraint.data.sourceObjects;
        sources.SetWeight(0, 0f);
        sources.SetWeight(1, 1f);
        weaponParentConstraint.data.sourceObjects = sources;
    }

    [PunRPC]
    public void SetWeaponParentConstraintHold()
    {
        var sources = weaponParentConstraint.data.sourceObjects;
        sources.SetWeight(0, 1f);
        sources.SetWeight(1, 0f);
        weaponParentConstraint.data.sourceObjects = sources;
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
    }

    private void ReduceBullets()
    {
        --weaponInHands.LoadedBullets;
        GameManager.Instance.AmmoText.text = weaponInHands.LoadedBullets + " / " + weaponInHands.BulletChamberSize;
    }

    private void SetAimingAnimation(bool aiming)
    {
        if (weaponInHands == null)
            return;

        string parameterName = "hand" + weaponInHands.WeaponType.ToString();
        anim.SetBool(parameterName, aiming);
    }

    private void SetArmAndHeadConstraint(bool shouldSetToAim)
    {
        if (shouldSetToAim)
        {
            leftArmTwoBoneConstraint.weight = 1f;
            rightArmTwoBoneConstraint.weight = 1f;
            headAimConstraint.weight = 0.7f;
        }
        else
        {
            leftArmTwoBoneConstraint.weight = 0f;
            rightArmTwoBoneConstraint.weight = 0f;
            headAimConstraint.weight = 0f;
        }
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
