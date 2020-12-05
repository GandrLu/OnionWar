using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] GameObject weaponPrefab;
    [SerializeField] Transform handHold;
    [SerializeField] Transform aimingPlane;
    private GameObject muzzleFlash;
    private PlayerMovement playerMovement;
    private LineRenderer aimingLine;
    private Animator anim;
    private PersonalWeapon weaponInHands;
    private ParticleSystem muzzleFlashParticles;
    private bool isAiming;
    private bool isReloading;
    private bool isReadyToFire;
    private int shootableMask;
    private float aimingDistance = 30f;
    private float timeToReload = 0.7f;        // The time between each shot.
    private float shotCooldownTimer;
    private float camRayLength = 100f;
    private Vector3 position;
    private Vector3 shotPosition;
    // RaycastHit variable to store information about what was hit by the aiming ray.
    private RaycastHit shootableHit;

    private void Awake()
    {
        shootableMask = LayerMask.GetMask("Shootable");
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        // Initially equip weapon
        ChangeWeapon(weaponPrefab);
    }

    private void Update()
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true
            || weaponInHands == null)
        {
            return;
        }
        // Store position for later methods
        position = transform.position;

        isAiming = Input.GetButton("Fire2");
        aimingLine.enabled = isAiming && isReadyToFire;

        ShotCooldown();

        SetAimingAnimation(isAiming);

        if (isAiming)
        {
            shotPosition = weaponInHands.ShootingPosition.position;
            Aim();
        }

        if (Input.GetButtonDown("Fire1") && isAiming && isReadyToFire)
            Shoot();
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
        shotCooldownTimer += timeToReload;
        isReadyToFire = false;
    }

    private void Shoot()
    {
        ResetShotCooldown();
        photonView.RPC("FlashMuzzle", RpcTarget.All);
        //FlashMuzzle();

        if (shootableHit.collider == null)
        {
            Ray shotRay = new Ray(shotPosition, transform.forward);
            // Perform the raycast and if it hits something on the shootable layer...
            if (Physics.Raycast(shotRay, out shootableHit, camRayLength, shootableMask))
            {
                var hitBox = shootableHit.collider.GetComponent<HitBox>();
                if (hitBox != null)
                {
                    hitBox.Hit();
                    Debug.Log("Shoot indirect " + shootableHit.collider.name);
                }
            }
        }
        else
        {
            
            Ray shotRay = new Ray(shotPosition, shootableHit.point - shotPosition);

            RaycastHit directShotHit;
            if (Physics.Raycast(shotRay, out directShotHit, camRayLength, shootableMask))
            {
                var hitBox = directShotHit.collider.GetComponent<HitBox>();
                if (hitBox != null)
                {
                    hitBox.Hit();
                    Debug.Log("Shoot direct " + directShotHit.collider.name);
                }
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

    [PunRPC]
    public void FlashMuzzle()
    {
        muzzleFlashParticles.Play();
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
        //if (!photonView.IsMine)
        //    return;
        GameObject weaponObj = Instantiate(weapon, handHold, false);
        //GameObject weaponObj = PhotonNetwork.Instantiate(weaponPrefab.name, Vector3.zero, Quaternion.identity);
        //weaponObj.transform.SetParent(handHold, false);
        weaponInHands = weaponObj.GetComponent<PersonalWeapon>();
        muzzleFlashParticles = weaponInHands.MuzzleFlash;
        aimingLine = weaponInHands.AimingLine;
        weaponInHands.SetHoldingTransform();
        aimingPlane.localPosition = new Vector3(0, weaponInHands.transform.position.y, 0);
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
}
