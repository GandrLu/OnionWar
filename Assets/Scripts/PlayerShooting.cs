using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviourPunCallbacks, IPunObservable
{
    public PlayerMovement player;
    [SerializeField] LineRenderer aimingLine;
    [SerializeField] GameObject muzzleFlash;
    ParticleSystem muzzleFlashParticles;
    private bool isAiming;
    private bool isReloading;
    private bool isReadyToFire;
    private int shootableMask;
    private float aimingDistance = 30f;
    private float timeToReload = 0.7f;        // The time between each shot.
    private float shotCooldownTimer;
    private float camRayLength = 100f;
    private Vector3 position;
    private Quaternion shootRotation;
    // RaycastHit variable to store information about what was hit by the aiming ray.
    private RaycastHit shootableHit;
    PhotonView photonView;

    private void Awake()
    {
        shootableMask = LayerMask.GetMask("Shootable");
        aimingLine = GetComponent<LineRenderer>();
        if (muzzleFlash == null)
            throw new MissingReferenceException();
        muzzleFlashParticles = muzzleFlash.GetComponent<ParticleSystem>();
        photonView = GetComponentInParent<PhotonView>();
    }

    private void Update()
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
        {
            return;
        }
        // Store position for later methods
        position = transform.position;

        isAiming = Input.GetButton("Fire2");
        aimingLine.enabled = isAiming && isReadyToFire;

        ShotCooldown();

        if (isAiming)
            Aim();
        else
            shootRotation = transform.rotation;

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
            Ray shotRay = new Ray(position, transform.forward);
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
            Ray shotRay = new Ray(position, shootableHit.point - position);

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
        aimingLine.SetPosition(0, position);
        // Create a ray from the mouse cursor on screen in the direction of the camera.
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Perform the raycast and if it hits something on the shootable layer...
        if (Physics.Raycast(camRay, out shootableHit, camRayLength, shootableMask))
        {
            Vector3 aimingVector = shootableHit.point - position;
            shootRotation = Quaternion.LookRotation(aimingVector);
            //Debug.Log("Pos " + playerToMouse);
            //Debug.Log("ScreenRay " + shootableHit.point);
            aimingLine.SetPosition(1, shootableHit.point);
        }
        else
        {
            aimingLine.SetPosition(1, position + transform.forward * aimingDistance);
            shootRotation = transform.rotation;
        }
    }

    [PunRPC]
    public void FlashMuzzle()
    {
        muzzleFlashParticles.Play();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        
    }
}
