using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPunCallbacks
{
    #region Serialized Fields
    [SerializeField] ControlScheme controlScheme;           // 0 is move in body direction, 1 is move in screen direction
    [SerializeField] float movementSpeed = 6f;              // The speed that the player will move at.
    [SerializeField] float aimingSlownessFactor = 0.6f;
    [SerializeField] float sprintFactor = 1.5f;
    #endregion

    #region Private Enums
    private enum ControlScheme
    {
        bodyDirect, screenAligned
    }
    #endregion

    #region Private Fields
    private Animator anim;
    private Camera mainCamera;
    private Rigidbody playerRigidbody;
    private PlayerShooting playerShooting;
    private Vector3 cameraRotation;
    private float horizontalAxisInput;
    private float verticalAxisInput;
    private float totalMovementSpeedSquared;
    private int aimingPlaneMask;                      // A layer mask for the aiming plane
    private bool isAiming;
    private bool isSprinting;
    private readonly float camRayLength = 100f;          // The length of the ray from the camera into the scene.
    #endregion

    #region Public Properties
    public float TotalMovementSpeedSquared { get => totalMovementSpeedSquared; set => totalMovementSpeedSquared = value; }
    #endregion

    #region Unity Callbacks
    void Awake()
    {
        // Create a layer mask for the aiming plane layer.
        aimingPlaneMask = LayerMask.GetMask("AimingPlane");

        // Set up references.
        anim = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerShooting = GetComponent<PlayerShooting>();

        if (anim == null)
            throw new MissingReferenceException();
        if (playerRigidbody == null)
            throw new MissingReferenceException();
        if (playerShooting == null)
            throw new MissingReferenceException();
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
            return;

        // Process input
        horizontalAxisInput = Input.GetAxisRaw("Horizontal");
        verticalAxisInput = Input.GetAxisRaw("Vertical");
        isSprinting = Input.GetButton("Sprint");
        isAiming = playerShooting.currentPlayerState == PlayerShooting.playerState.aiming;
        // Change control scheme with 1 and 2 on alphabetical keyboard
        if (Input.GetButton("MovementDirectScheme"))
            controlScheme = ControlScheme.bodyDirect;
        else if (Input.GetButton("MovementScreenScheme"))
            controlScheme = ControlScheme.screenAligned;

        cameraRotation = mainCamera.transform.rotation.eulerAngles;
    }

    void FixedUpdate()
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
            return;

        // Move the player around the scene.
        Move();

        // Turn the player to face the mouse cursor.
        Turning();

        // Animate the player.
        Animating();
    }
    #endregion

    #region Private Methods
    private void Animating()
    {
        // Determine if character moves
        bool isWalking = IsWalking(horizontalAxisInput, verticalAxisInput);

        // Tell the animator whether or not the player is walking etc.
        anim.SetBool("IsWalking", isWalking);
        anim.SetBool("IsAiming", isAiming);
        anim.SetFloat("vertical", verticalAxisInput);
        anim.SetFloat("horizontal", horizontalAxisInput);
    }

    private void ApplyAimedMovement()
    {
        if (controlScheme == ControlScheme.bodyDirect)
        {
            var absoluteMovement = new Vector3(horizontalAxisInput, 0, verticalAxisInput).normalized;
            absoluteMovement *= Time.fixedDeltaTime * movementSpeed * aimingSlownessFactor;
            TotalMovementSpeedSquared = absoluteMovement.sqrMagnitude;

            var angle = Vector3.SignedAngle(Vector3.forward, transform.forward, Vector3.up);
            Vector3 alignedMovement = Quaternion.Euler(0, angle, 0) * absoluteMovement;

            playerRigidbody.MovePosition(playerRigidbody.position + alignedMovement);
        }
        else if (controlScheme == ControlScheme.screenAligned)
        {
            var movement = new Vector3(horizontalAxisInput, 0, verticalAxisInput).normalized;
            movement *= Time.fixedDeltaTime * movementSpeed * aimingSlownessFactor;
            TotalMovementSpeedSquared = movement.sqrMagnitude;

            var alignedMovement = Quaternion.Euler(0, cameraRotation.y, 0) * movement;
            playerRigidbody.MovePosition(playerRigidbody.position + alignedMovement);
        }
    }

    private void ApplyUnaimedMovement()
    {
        if (controlScheme == ControlScheme.bodyDirect)
        {
            var movement = transform.forward * Mathf.Abs(verticalAxisInput) + transform.forward * Mathf.Abs(horizontalAxisInput);
            movement.Normalize();
            movement *= movementSpeed * Time.fixedDeltaTime;
            if (isSprinting)
            {
                movement *= sprintFactor;
            }
            TotalMovementSpeedSquared = movement.sqrMagnitude;

            playerRigidbody.MovePosition(playerRigidbody.position + movement);
        }
        else if (controlScheme == ControlScheme.screenAligned)
        {
            var movement = new Vector3(horizontalAxisInput, 0, verticalAxisInput).normalized;
            movement *= movementSpeed * Time.fixedDeltaTime;
            if (isSprinting)
            {
                movement *= sprintFactor;
            }
            TotalMovementSpeedSquared = movement.sqrMagnitude;

            var alignedMovement = Quaternion.Euler(0, cameraRotation.y, 0) * movement;
            playerRigidbody.MovePosition(playerRigidbody.position + alignedMovement);
        }
    }

    private bool IsWalking(float h, float v)
    {
        return v != 0f || h != 0f;
    }

    private void Move()
    {
        if (isAiming)
            ApplyAimedMovement();
        else
            ApplyUnaimedMovement();
    }

    private void Turning()
    {
        if (isAiming)
        {
            // Create a ray from the mouse cursor on screen in the direction of the camera.
            Ray camRay = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Perform the raycast and if it hits something on the aiming plane layer...
            if (Physics.Raycast(camRay, out RaycastHit floorHit, camRayLength, aimingPlaneMask))
            {
                // Create a vector from the player to the point on the aiming plane the raycast from the mouse hit.
                Vector3 playerToMouse = floorHit.point - transform.position;
                // Ensure the vector is entirely horizontal
                playerToMouse.y = 0f;

                // Set the player's rotation to this new rotation.
                transform.forward = playerToMouse;
            }
        }
        else if (controlScheme == ControlScheme.bodyDirect)
        {
            // Create a ray from the mouse cursor on screen in the direction of the camera.
            Ray camRay = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Perform the raycast and if it hits something on the aiming plane layer...
            if (Physics.Raycast(camRay, out RaycastHit floorHit, camRayLength, aimingPlaneMask))
            {
                // Create a vector from the player to the point on the aiming plane the raycast from the mouse hit.
                Vector3 playerToMouse = floorHit.point - transform.position;
                // Ensure the vector is entirely horizontal
                playerToMouse.y = 0f;
                // Normalize it to calculate it with input
                playerToMouse.Normalize();

                // Set the player's rotation to this new rotation.
                Vector3 alignedRotate;
                if (horizontalAxisInput != 0 || verticalAxisInput != 0)
                    alignedRotate = Quaternion.LookRotation(playerToMouse) * new Vector3(horizontalAxisInput, 0, verticalAxisInput).normalized;
                else
                    alignedRotate = Quaternion.LookRotation(playerToMouse) * Vector3.forward;

                if (alignedRotate != Vector3.zero)
                    transform.forward = alignedRotate;
            }
        }
        else if (controlScheme == ControlScheme.screenAligned)
        {
            var rotate = new Vector3(horizontalAxisInput, 0, verticalAxisInput);
            var alignedRotate = Quaternion.Euler(0, cameraRotation.y, 0) * rotate;
            if (alignedRotate != Vector3.zero)
                transform.forward = alignedRotate;
        }
    }
    #endregion
}
