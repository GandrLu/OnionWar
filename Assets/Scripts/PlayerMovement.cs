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
            Camera.main.GetComponent<CameraFollow>().Target = transform;
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

        // Change control scheme with 1 and 2 on alphabetical keyboard
        if (Input.GetKeyDown(KeyCode.Alpha1))
            controlScheme = ControlScheme.bodyDirect;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
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
        if (aiming || controlScheme == ControlScheme.bodyDirect)
        {
            // Create a ray from the mouse cursor on screen in the direction of the camera.
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 lookDirection = worldPoint - playerRigidbody.position;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg - 90f;

            // Create a RaycastHit variable to store information about what was hit by the ray.
            RaycastHit floorHit;

            // Perform the raycast and if it hits something on the floor layer...
            if (Physics.Raycast(camRay, out floorHit, camRayLength, aimingPlaneMask))
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
        // Mousewheel
        else if (Input.GetButton("Fire3"))
        {
            Debug.Log(Input.mousePosition);
            if (Input.mousePosition.x < m_ScreenWidth / 2 - 10)
            {
                this.transform.Rotate(Vector3.up, -5f);
                Debug.Log("rotate+");
            }
            else if (Input.mousePosition.x > m_ScreenWidth / 2 + 10)
            {
                this.transform.Rotate(Vector3.up, 5f);
                Debug.Log("rotate-");
            }
        }
        else if (controlScheme == ControlScheme.screenAligned)
        {
            var rotate = new Vector3(h, 0, v);
            if (rotate != Vector3.zero)
                transform.forward = rotate;
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

            var angle = Vector3.SignedAngle(Vector3.forward, transform.forward, Vector3.up);
            Vector3 alignedMovement = Quaternion.Euler(0, angle, 0) * absoluteMovement;

            playerRigidbody.MovePosition(playerRigidbody.position + alignedMovement);
        }
        else if (controlScheme == ControlScheme.screenAligned)
        {
            movement = new Vector3(h, 0, v).normalized;
            movement *= Time.fixedDeltaTime * m_Speed * aimingSlownessFactor;
            playerRigidbody.MovePosition(playerRigidbody.position + movement);

        }
    }

    private void ApplyUnaimedMovement(float horizontal, float vertical)
    {
        if (controlScheme == ControlScheme.bodyDirect)
        {
            if (m_IsSprinting)
            {
                vertical *= sprintFactor;
                //horizontal *= sprintFactor;
            }
            playerRigidbody.MovePosition(playerRigidbody.position + transform.forward * vertical * m_Speed * Time.fixedDeltaTime);
        }
        else if (controlScheme == ControlScheme.screenAligned)
        {
            movement = new Vector3(h, 0, v).normalized;
            movement *= m_Speed * Time.fixedDeltaTime;
            if (m_IsSprinting)
            {
                movement *= sprintFactor;
            }
            playerRigidbody.MovePosition(playerRigidbody.position + movement);
        }
    }
    private bool IsWalking(float h, float v)
    {
        if (controlScheme == ControlScheme.bodyDirect)
            return v != 0f;
        else
            return v != 0f || h != 0f;
    }
}
