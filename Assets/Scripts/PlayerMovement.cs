using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPunCallbacks, IPunObservable
{
    enum ControlScheme
    {
        bodyDirect,
        screenAligned
    }

    [SerializeField] float m_Speed = 6f;            // The speed that the player will move at.
    [SerializeField] float aimingSlownessFactor = 0.6f;
    [SerializeField] float sprintFactor = 1.5f;
    private Vector3 movement;                   // The vector to store the direction of the player's movement.
    private Vector3 cameraRotation;
    private Animator anim;                      // Reference to the animator component.
    private Rigidbody playerRigidbody;          // Reference to the player's rigidbody.
    private int aimingPlaneMask;                      // A layer mask so that a ray can be cast just at gameobjects on the floor layer.
    private float camRayLength = 100f;          // The length of the ray from the camera into the scene.
    //private bool isAiming = false;
    private bool m_IsSprinting = false;
    private int m_ScreenWidth = 0;
    private float h;
    private float v;
    private bool isAiming;
    public Vector3 aimingAtShootableDirection;
    public bool aimingAtShootable;
    [SerializeField] Transform IkTargetRight;
    [SerializeField] Transform IkTargetLeft;
    [SerializeField] Transform Spine;
    Vector3 oldPosition = Vector3.zero;
    Quaternion oldRotation = Quaternion.identity;
    // 0 is move in body direction, 1 is move in screen direction
    [SerializeField] ControlScheme controlScheme;
    private Camera mainCamera;
    private float speed;

    public float Speed { get => speed; set => speed = value; }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

    }

    void Awake()
    {
        // Create a layer mask for the floor layer.
        aimingPlaneMask = LayerMask.GetMask("AimingPlane");

        // Set up references.
        anim = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();

        m_ScreenWidth = Screen.width;
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            mainCamera = Camera.main;
            mainCamera.GetComponent<CameraFollow>().Target = transform;
        }
    }

    void Update()
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
        {
            return;
        }
        // Store the input axes.
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        isAiming = Input.GetButton("Fire2");
        m_IsSprinting = Input.GetButton("Sprint");
        cameraRotation = mainCamera.transform.rotation.eulerAngles;

        // Change control scheme with 1 and 2 on alphabetical keyboard
        if (Input.GetButton("MovementDirectScheme"))
            controlScheme = ControlScheme.bodyDirect;
        else if (Input.GetButton("MovementScreenScheme"))
            controlScheme = ControlScheme.screenAligned;
    }


    void FixedUpdate()
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
        {
            return;
        }
        // Move the player around the scene.
        Move(h, v, isAiming);

        // Turn the player to face the mouse cursor.
        Turning(isAiming);

        // Animate the player.
        Animating(h, v, isAiming);
    }

    void Move(float h, float v, bool aiming)
    {
        if (aiming)
            ApplyAimedMovement(h, v);
        else
            ApplyUnaimedMovement(h, v);
    }

    void Turning(bool aiming)
    {
        if (aiming)
        {
            // Create a ray from the mouse cursor on screen in the direction of the camera.
            Ray camRay = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Perform the raycast and if it hits something on the floor layer...
            if (Physics.Raycast(camRay, out RaycastHit floorHit, camRayLength, aimingPlaneMask))
            {
                // Create a vector from the player to the point on the floor the raycast from the mouse hit.
                Vector3 playerToMouse = floorHit.point - transform.position;

                // Ensure the vector is entirely along the floor plane.
                playerToMouse.y = 0f;

                // Create a quaternion (rotation) based on looking down the vector from the player to the mouse.
                Quaternion newRotation = Quaternion.LookRotation(playerToMouse);

                // Set the player's rotation to this new rotation.
                playerRigidbody.MoveRotation(newRotation);
            }
        }
        else if (controlScheme == ControlScheme.bodyDirect)
        {
            // Create a ray from the mouse cursor on screen in the direction of the camera.
            Ray camRay = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Perform the raycast and if it hits something on the floor layer...
            if (Physics.Raycast(camRay, out RaycastHit floorHit, camRayLength, aimingPlaneMask))
            {
                // Create a vector from the player to the point on the floor the raycast from the mouse hit.
                Vector3 playerToMouse = floorHit.point - transform.position;

                // Ensure the vector is entirely along the floor plane.
                playerToMouse.y = 0f;
                playerToMouse.Normalize();

                // Set the player's rotation to this new rotation.
                Vector3 alignedRotate;
                if (h != 0 || v != 0)
                    alignedRotate = Quaternion.LookRotation(playerToMouse) * new Vector3(h, 0, v).normalized;
                else
                    alignedRotate = Quaternion.LookRotation(playerToMouse) * Vector3.forward;
                
                if (alignedRotate != Vector3.zero)
                    transform.forward = alignedRotate;
            }
        }
        else if (controlScheme == ControlScheme.screenAligned)
        {
            var rotate = new Vector3(h, 0, v);
            var alignedRotate = Quaternion.Euler(0, cameraRotation.y, 0) * rotate;
            if (alignedRotate != Vector3.zero)
                transform.forward = alignedRotate;
        }
    }

    void Animating(float h, float v, bool aiming)
    {
        // Create a boolean that is true if either of the input axes is non-zero.
        // Only considering vertical movement because in walking mode there is 
        // no sideway movement.
        bool walking = IsWalking(h, v);

        // Tell the animator whether or not the player is walking.
        anim.SetBool("IsWalking", walking);
        anim.SetBool("IsAiming", aiming);
        anim.SetFloat("vertical", v);
        anim.SetFloat("horizontal", h);
    }

    private void ApplyAimedMovement(float horizontal, float vertical)
    {
        if (controlScheme == ControlScheme.bodyDirect)
        {
            var absoluteMovement = new Vector3(horizontal, 0, vertical).normalized;
            absoluteMovement *= Time.fixedDeltaTime * m_Speed * aimingSlownessFactor;
            Speed = absoluteMovement.sqrMagnitude;

            var angle = Vector3.SignedAngle(Vector3.forward, transform.forward, Vector3.up);
            Vector3 alignedMovement = Quaternion.Euler(0, angle, 0) * absoluteMovement;

            playerRigidbody.MovePosition(playerRigidbody.position + alignedMovement);
        }
        else if (controlScheme == ControlScheme.screenAligned)
        {
            movement = new Vector3(h, 0, v).normalized;
            movement *= Time.fixedDeltaTime * m_Speed * aimingSlownessFactor;
            Speed = movement.sqrMagnitude;

            var alignedMovement = Quaternion.Euler(0, cameraRotation.y, 0) * movement;
            playerRigidbody.MovePosition(playerRigidbody.position + alignedMovement);
        }
    }

    private void ApplyUnaimedMovement(float horizontal, float vertical)
    {
        if (controlScheme == ControlScheme.bodyDirect)
        {
            movement = transform.forward * Mathf.Abs(vertical) + transform.forward * Mathf.Abs(horizontal);
            movement.Normalize();
            movement *= m_Speed * Time.fixedDeltaTime;
            if (m_IsSprinting)
            {
                movement *= sprintFactor;
            }
            Speed = movement.sqrMagnitude;

            playerRigidbody.MovePosition(playerRigidbody.position + movement);
        }
        else if (controlScheme == ControlScheme.screenAligned)
        {
            movement = new Vector3(h, 0, v).normalized;
            movement *= m_Speed * Time.fixedDeltaTime;
            if (m_IsSprinting)
            {
                movement *= sprintFactor;
            }
            Speed = movement.sqrMagnitude;

            var alignedMovement = Quaternion.Euler(0, cameraRotation.y, 0) * movement;
            playerRigidbody.MovePosition(playerRigidbody.position + alignedMovement);
        }
    }
    private bool IsWalking(float h, float v)
    {
            return v != 0f || h != 0f;
    }
}
