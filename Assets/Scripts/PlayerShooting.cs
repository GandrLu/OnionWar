using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField]
    private LineRenderer aimingLine;
    private float aimingDistance = 30f;
    [SerializeField]
    private GameObject projectile;
    private bool isAiming;
    private float timeBetweenBullets = 0.5f;        // The time between each shot.
    private float timer;
    private int shootableMask;
    private float camRayLength = 100f;
    private Vector3 position;
    Quaternion shootRotation;
    //public PlayerMovement player;

    private void Awake()
    {
        shootableMask = LayerMask.GetMask("Shootable");
        aimingLine = GetComponent<LineRenderer>();
    }

    //private void Start()
    //{
    //}

    private void Update()
    {
        timer += Time.deltaTime;
        position = transform.position;

        isAiming = Input.GetButton("Fire2");
        aimingLine.enabled = isAiming;

        if (isAiming)
            Aim();
        else
            shootRotation = transform.rotation;

        if (Input.GetButton("Fire1") && timer >= timeBetweenBullets)
            Shoot();
    }

    private void Shoot()
    {
        timer = 0f;
        Instantiate(projectile, position, shootRotation);
    }

    private void Aim()
    {
        aimingLine.SetPosition(0, position);
        // Create a ray from the mouse cursor on screen in the direction of the camera.
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Create a RaycastHit variable to store information about what was hit by the ray.
        RaycastHit shootableHit;

        // Perform the raycast and if it hits something on the floor layer...
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
        }
    }
}
