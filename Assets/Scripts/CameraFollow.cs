using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    [SerializeField] float zAxisCameraOffset = 7.5f;
    [SerializeField] float maxCameraHeight = 15f;
    [SerializeField] float minCameraHeight = 6f;
    [SerializeField] float smoothing = 5f;
    [SerializeField] float rotationSpeed = 50f;
    [SerializeField] float heightChangeSpeed = 20f;
    [SerializeField] Transform target;
    private bool is_FollowActive;
    private int m_ScreenWidth = 0;
    private Vector3 offset;

    public Transform Target
    {
        get => target;
        set
        {
            target = value;
            if (target != null)
            {
                is_FollowActive = true;
                Initialize();
            }
            else
                is_FollowActive = false;
        }
    }

    void Start()
    {
        var aspect = Camera.main.aspect;
        // 1680 x 1050
        if (Mathf.Abs(aspect - 1.6f) <= 0.01)
            Camera.main.fieldOfView = 65;
        // 1920 x 1080
        else if (Mathf.Abs(aspect - 1.777f) <= 0.001)
            Camera.main.fieldOfView = 60;
        // 4:3
        else if (Mathf.Abs(aspect - 1.5f) <= 0.001)
            Camera.main.fieldOfView = 69;

        if (!is_FollowActive)
            return;
        Initialize();
    }

    void Update()
    {
        if (!is_FollowActive)
            return;

        // Must run before rotation
        if (Target != null)
        {
            Vector3 targetCamPos = Target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetCamPos, smoothing + Time.deltaTime);
        }

        // Camera rotation (must run before height)
        bool rotateLeft = Input.GetButton("CamRotateLeft");
        bool rotateRight = Input.GetButton("CamRotateRight");
        if (rotateLeft && !rotateRight)
        {
            transform.RotateAround(target.position, Vector3.up, -rotationSpeed * Time.deltaTime);
            offset = transform.position - target.position;
        }
        if (rotateRight && !rotateLeft)
        {
            transform.RotateAround(target.position, Vector3.up, rotationSpeed * Time.deltaTime);
            offset = transform.position - target.position;
        }

        // Camera height
        var scrollWheel = Input.GetAxis("Mouse ScrollWheel");
        var heightChange = transform.forward * scrollWheel * heightChangeSpeed * heightChangeSpeed * Time.deltaTime;
        if (offset.y < maxCameraHeight && scrollWheel < 0)
            offset += heightChange;
        if (offset.y > minCameraHeight && scrollWheel > 0)
            offset += heightChange;

        transform.LookAt(Target);
    }

    private void Initialize()
    {
        transform.position = new Vector3(Target.position.x, transform.position.y, Target.position.z - zAxisCameraOffset);
        transform.rotation.Set(0, Target.rotation.y, 0, 0);
        m_ScreenWidth = Screen.width;
        offset = transform.position - Target.position;
    }
}
