using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Serialized Fields
    [SerializeField] bool forceAiming;
    [SerializeField] List<GameObject> weaponPrefabs;
    [SerializeField] Transform aimingPlane;
    [SerializeField] Transform aimPosition;
    [SerializeField] Transform backPosition;
    [SerializeField] Transform holdPositionTwohanded;
    [SerializeField] Transform holdPositionOnehanded;

    [SerializeField] bool isIkActive;
    [SerializeField] Transform IkTargetRight;
    [SerializeField] Transform IkTargetLeft;
    [SerializeField] Transform IkTargetHead;

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
    private LineRenderer aimingLine1;
    private LineRenderer aimingLine2;
    private PersonalWeapon weaponInHands;
    private ParticleSystem muzzleFlashParticles;
    // RaycastHit variable to store information about what was hit by the aiming ray.
    private RaycastHit shootableHit;

    private bool isAiming, isReloading, isReadyToFire;
    private int shootableMask;
    private int shootableAndFloorMask;
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
                SetIKsToAiming(isAiming);
            aimHoldRotation = (Vector3)stream.ReceiveNext();
            if (isAiming)
                aimPosition.forward = aimHoldRotation;
        }
    }
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        shootableMask = LayerMask.GetMask(new string[] { "Default", "HitBox" });
        shootableAndFloorMask = LayerMask.GetMask(new string[] { "Default", "HitBox", "Floor" });
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        playerMovement = GetComponent<PlayerMovement>();
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
            UpdateHUDAmmoText();
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
        isAiming = Input.GetButton("Fire2") || forceAiming;
        aimingLine1.enabled = isAiming && isReadyToFire && !isReloading;
        aimingLine2.enabled = isAiming && isReadyToFire && !isReloading;

        if (isAiming)
        {
            shotPosition = weaponInHands.ShootingPosition.position;
            Aim();
            SetIKsToAiming(true);
        }
        else
            SetIKsToAiming(false);


        // Shooting
        if (weaponInHands.HasAutomaticFire)
        {
        if (Input.GetButton("Fire1") && isAiming && isReadyToFire && !isReloading)
            Shoot();
        }
        else
        {
        if (Input.GetButtonDown("Fire1") && isAiming && isReadyToFire && !isReloading)
            Shoot();
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (aimingLine1 != null)
            aimingLine1.enabled = false;
        if (aimingLine2 != null)
            aimingLine2.enabled = false;
    }

    void OnAnimatorIK()
    {
        if (anim)
        {
            //if the IK is active, set the position and rotation directly to the goal. 
            if (isIkActive)
            {
                // Set the look target position, if one has been assigned
                if (IkTargetHead != null)
                {
                    anim.SetLookAtWeight(1);
                    anim.SetLookAtPosition(IkTargetHead.position);
                }
                // Set the right hand target position and rotation, if one has been assigned
                if (IkTargetRight != null)
                {
                    anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                    anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                    anim.SetIKPosition(AvatarIKGoal.RightHand, IkTargetRight.position);
                    anim.SetIKRotation(AvatarIKGoal.RightHand, IkTargetRight.rotation);
                }
                // Set the left hand target position and rotation, if one has been assigned
                if (IkTargetLeft != null)
                {
                    anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                    anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                    anim.SetIKPosition(AvatarIKGoal.LeftHand, IkTargetLeft.position);
                    anim.SetIKRotation(AvatarIKGoal.LeftHand, IkTargetLeft.rotation);
                }
            }
            //if the IK is not active, set the position and rotation of the hand and head back to the original position
            else
            {
                anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
                anim.SetLookAtWeight(0);
            }
        }
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
        UpdateHUDAmmoText();
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
        GameObject weaponObj = Instantiate(weaponPrefabs[weaponIndex], backPosition);
        weaponInHands = weaponObj.GetComponent<PersonalWeapon>();
        SetupIkTargets();

        muzzleFlashParticles = weaponInHands.MuzzleFlash;
        aimingLine1 = weaponInHands.AimingLine1;
        aimingLine2 = weaponInHands.AimingLine2;
        aimingPlane.localPosition = new Vector3(0, aimPosition.transform.position.y, 0);
        ActiveWeaponIndex = weaponIndex;
        GameManager.Instance.ItemImage.sprite = weaponInHands.HudImage;
        UpdateHUDAmmoText();

        if (weaponInHands.WeaponType == PersonalWeaponType.Rifle)
        {
            anim.SetBool("hold" + PersonalWeaponType.Rifle.ToString(), true);
            anim.SetBool("hold" + PersonalWeaponType.Pistol, false);
        }
        else if (weaponInHands.WeaponType == PersonalWeaponType.Pistol)
        {
            anim.SetBool("hold" + PersonalWeaponType.Pistol, true);
            anim.SetBool("hold" + PersonalWeaponType.Rifle.ToString(), false);
        }
    }

    [PunRPC]
    public void PlayShotEffects()
    {
        muzzleFlashParticles.Play();
        audioSource.PlayOneShot(weaponInHands.AudioClip);
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

    private void SetIKsToAiming(bool shouldSetToAim)
    {
        isIkActive = shouldSetToAim;
        if (shouldSetToAim)
        {
            weaponInHands.transform.SetParent(aimPosition, false);
        }
        else
        {
            if (weaponInHands.WeaponType == PersonalWeaponType.Rifle)
                weaponInHands.transform.SetParent(holdPositionTwohanded, false);
            else if (weaponInHands.WeaponType == PersonalWeaponType.Pistol)
                weaponInHands.transform.SetParent(holdPositionOnehanded, false);
        }
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
            if (Physics.Raycast(shotRay, out shootableHit, weaponInHands.Range, shootableAndFloorMask))
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
            if (Physics.Raycast(shotRay, out directShotHit, weaponInHands.Range, shootableAndFloorMask))
            {
                var hitBox = directShotHit.collider.GetComponent<HitBox>();
                if (hitBox != null)
                    hitBox.Hit(directShotHit.point, Quaternion.FromToRotation(Vector3.forward, shotDirection));
            }
        }
    }

    private void Aim()
    {
        aimingLine1.SetPosition(0, shotPosition);
        // Create a ray from the mouse cursor on screen in the direction of the camera.
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 shotTarget = new Vector3();
        // Perform the raycast and if it hits something on the shootable layer...
        if (Physics.Raycast(camRay, out shootableHit, camRayLength, shootableMask))
        {
            Vector3 aimingVector = shootableHit.point - shotPosition;
            //Debug.Log("Pos " + playerToMouse);
            //Debug.Log("ScreenRay " + shootableHit.point);
            aimingLine2.SetPosition(1, shootableHit.point);
            shotTarget = shootableHit.point;
        }
        else
        {
            aimingLine2.SetPosition(1, shotPosition + transform.forward * aimingDistance);
            shotTarget = shotPosition + transform.forward * aimingDistance;
        }
        RaycastHit aimingPos;
        if (Physics.Raycast(shotPosition, shotTarget - shotPosition, out aimingPos, weaponInHands.Range, shootableAndFloorMask))
        {
            aimingLine1.SetPosition(1, aimingPos.point);
            aimingLine2.SetPosition(0, aimingPos.point);
        }
        else
        {
            aimingLine1.SetPosition(1, shotTarget);
            aimingLine2.SetPosition(0, shotTarget);
        }
        aimPosition.forward = shotTarget - shotPosition;
        aimHoldRotation = shotTarget - shotPosition;
    }

    private void ReduceBullets()
    {
        --weaponInHands.LoadedBullets;
        UpdateHUDAmmoText();
    }

    private void UpdateHUDAmmoText()
    {
        GameManager.Instance.AmmoText.text = weaponInHands.LoadedBullets + " / " + weaponInHands.BulletChamberSize;
    }

    private void SetupIkTargets()
    {
        IkTargetLeft = weaponInHands.RefLeft;
        IkTargetRight = weaponInHands.RefRight;
        IkTargetHead = weaponInHands.ShootingPosition;
    }
    #endregion
}
