using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] GameObject weaponPrefab;
    [SerializeField] Transform handHold;
    [SerializeField] Transform aimingPlane;
    [SerializeField] float inaccuracyAcceleration = 2f;
    [SerializeField] float inaccuracyDeceleration = 0.75f;
    [SerializeField] float moveSpeedInaccuracyImpact = 3f;
    [SerializeField] float inaccuracyShootImpact = 5f;

    private Animator anim;
    private AudioSource audioSource;
    private PlayerMovement playerMovement;
    private LineRenderer aimingLine;
    private PersonalWeapon weaponInHands;
    private ParticleSystem muzzleFlashParticles;
    // RaycastHit variable to store information about what was hit by the aiming ray.
    private RaycastHit shootableHit;
    private bool isAiming, isReloading, isReadyToFire;
    private int shootableMask;
    private float aimingDistance = 30f;
    private float camRayLength = 100f;
    private float currentInaccuracy;
    private float timeToReload = 0.7f;        // The time between each shot.
    private float reloadCooldownTimer, shotCooldownTimer;
    private float playerMoveSpeed;
    private Vector3 shotPosition;

    private void Awake()
    {
        shootableMask = LayerMask.GetMask(new string[] { "Default", "HitBox" });
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        playerMovement = GetComponent<PlayerMovement>();
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
        }

        // Shooting
        if (Input.GetButtonDown("Fire1") && isAiming && isReadyToFire && !isReloading)
            Shoot();
    }

    private void HandleInaccuracy()
    {
        playerMoveSpeed = playerMovement.Speed * moveSpeedInaccuracyImpact;

        // Inaccuracy is increasing
        if (playerMoveSpeed > currentInaccuracy)
            currentInaccuracy =
                Mathf.Lerp(currentInaccuracy, playerMoveSpeed, inaccuracyAcceleration * Time.deltaTime);
        // Inaccuracy is decreasing
        else
            currentInaccuracy =
                Mathf.Lerp(currentInaccuracy, playerMoveSpeed, inaccuracyDeceleration * Time.deltaTime);

        if (currentInaccuracy < 0.00001f)
            currentInaccuracy = 0f;
        CursorManager.Instance.ResizeCrosshair(currentInaccuracy);
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
        var x = inaccuracyShootImpact *
            Random.Range(-weaponInHands.Accuracy - currentInaccuracy, weaponInHands.Accuracy + currentInaccuracy);
        var y = inaccuracyShootImpact *
            Random.Range(-weaponInHands.Accuracy - currentInaccuracy, weaponInHands.Accuracy + currentInaccuracy);

        ResetShotCooldown();
        photonView.RPC(nameof(PlayShotEffects), RpcTarget.All);
        ReduceBullets();
        currentInaccuracy += weaponInHands.Recoil;

        if (shootableHit.collider == null)
        {
            var shotDirection = transform.forward + new Vector3(x, y, 0);
            //Debug.DrawRay(shotPosition, shotDirection * 20, Color.blue, 60);
            Ray shotRay = new Ray(shotPosition, shotDirection);
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

        // Perform the raycast and if it hits something on the shootable layer...
        if (Physics.Raycast(camRay, out shootableHit, camRayLength, shootableMask))
        {
            Vector3 aimingVector = shootableHit.point - shotPosition;
            //Debug.Log("Pos " + playerToMouse);
            //Debug.Log("ScreenRay " + shootableHit.point);
            aimingLine.SetPosition(1, shootableHit.point);
        }
        else
        {
            aimingLine.SetPosition(1, shotPosition + transform.forward * aimingDistance);
        }
    }

    private void SetAimingAnimation(bool aiming)
    {
        if (weaponInHands == null)
            return;

        if (aiming)
            weaponInHands.SetAimingTransform();
        else
            weaponInHands.SetHoldingTransform();

        string parameterName = "hand" + weaponInHands.WeaponType.ToString();
        anim.SetBool(parameterName, aiming);
    }

    private void ReduceBullets()
    {
        --weaponInHands.LoadedBullets;
        GameManager.Instance.AmmoText.text = weaponInHands.LoadedBullets + " / " + weaponInHands.BulletChamberSize;
    }

    [PunRPC]
    public void PlayShotEffects()
    {
        muzzleFlashParticles.Play();
        audioSource.PlayOneShot(audioSource.clip);
    }

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
        GameObject weaponObj = Instantiate(weapon, handHold, false);
        weaponInHands = weaponObj.GetComponent<PersonalWeapon>();
        muzzleFlashParticles = weaponInHands.MuzzleFlash;
        aimingLine = weaponInHands.AimingLine;
        weaponInHands.SetHoldingTransform();
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
}
