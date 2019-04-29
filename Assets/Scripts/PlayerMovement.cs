using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPunCallbacks, IPunObservable
{
    public float m_Speed = 6f;            // The speed that the player will move at.

    //Vector3 movement;                   // The vector to store the direction of the player's movement.
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
    [SerializeField]
    private GameObject weaponPrefab;
    [SerializeField]
    private Transform handHold;
    private PersonalWeapon weaponInHands;
    public Transform aimingPlane;
    public Vector3 aimingAtShootableDirection;
    public bool aimingAtShootable;
    [SerializeField] Transform IkTargetRight;
    [SerializeField] Transform IkTargetLeft;
    [SerializeField] Transform Spine;
    Vector3 oldPosition = Vector3.zero;
    Quaternion oldRotation = Quaternion.identity;

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
        //if (stream.IsWriting)
        //{
        //    stream.SendNext(transform.position);
        //    stream.SendNext(transform.rotation);
        //    //stream.SendNext(playerRigidbody.velocity);
        //}
        //else
        //{
        //    var movement = transform.position - oldPosition;
        //    oldPosition = (Vector3)stream.ReceiveNext();
        //    oldRotation = (Quaternion)stream.ReceiveNext();
            
        //    //playerRigidbody.position = (Vector3)stream.ReceiveNext();
        //    //playerRigidbody.rotation = (Quaternion)stream.ReceiveNext();
        //    //playerRigidbody.velocity = (Vector3)stream.ReceiveNext();

        //    float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
        //    //playerRigidbody.position += playerRigidbody.velocity * lag;
            
        //    Vector3 networkPosition = lag * movement;
        //    transform.position = Vector3.MoveTowards(transform.position, networkPosition, m_Speed * Time.deltaTime);
        //}
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
        ChangeWeapon(weaponPrefab);
    }

    void Update(){
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
	{
            return;
        }

		// Store the input axes.
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        isAiming = Input.GetButton("Fire2");
        m_IsSprinting = Input.GetButton("Sprint");
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
        Turning(true);

		// Animate the player.
        Animating(h, v, isAiming);
	}

    void Move(float h, float v, bool aiming)
    {
        float horizontalMovement = h * m_Speed * Time.deltaTime * 0.6f;
        float verticalMovement = v * m_Speed * Time.deltaTime;

        if (aiming)
	{
        // Set the movement vector based on the axis input.
            //movement.Set(h, 0f, v);

        // Normalize the movement vector and make it proportional to the speed per second.
            //movement = movement.normalized * m_Speed * Time.deltaTime;
        //playerRigidbody.AddRelativeForce(speed * h, 0, speed * v)

            // Slow movement while aiming
            verticalMovement *= 0.5f;
            horizontalMovement *= 0.5f;

        // Move the player to it's current position plus the movement.
        //playerRigidbody.MovePosition (new Vector3(transform.localPosition.x * horizontalMovement, 0, transform.localPosition.z * verticalMovement));
        transform.localPosition += transform.forward * verticalMovement;
        transform.localPosition += transform.right * horizontalMovement;
        }
        else
        {
            if (m_IsSprinting)
            {
                verticalMovement *= 2f;
                horizontalMovement *= 2f;
            }
            transform.localPosition += transform.forward * verticalMovement;
            //transform.localPosition += transform.right * horizontalMovement;
        }
    }

    void Turning(bool aiming)
    {
        if (aiming)
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
        else
        {
//            if (Input.GetButtonDown("Horizontal") || Input.GetButtonDown("Vertical"))
//            {
//                transform.rotation = Quaternion.LookRotation(new Vector3(h, 0, v));
//            }
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 lookDirection = worldPoint - playerRigidbody.position;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg - 90f;
            Quaternion rotate = Quaternion.AngleAxis(angle, Vector3.up);
            playerRigidbody.rotation = rotate;
            Debug.Log(worldPoint);
		}
	}

    void Animating(float h, float v, bool aiming)
	{
		// Create a boolean that is true if either of the input axes is non-zero.
        // Only considering vertical movement because in walking mode there is 
        // no sideway movement.
		bool walking = v != 0f;

        SetAimingAnimation(aiming);

		// Tell the animator whether or not the player is walking.
        anim.SetBool("IsWalking", walking);
        anim.SetBool("IsAiming", aiming);
        anim.SetFloat("vertical", v);
        anim.SetFloat("horizontal", h);
    }

    public void ChangeWeapon(GameObject weapon)
    {
        GameObject weaponObj = Instantiate(weapon, handHold, false);
        weaponObj.GetComponentInChildren<PlayerShooting>().player = this;
        weaponInHands = weaponObj.GetComponent<PersonalWeapon>();
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
     
        string parameterName = "hand" + weaponInHands.GetWeaponType().ToString();
        anim.SetBool(parameterName, aiming);
    }

    private void OnAnimatorIK()
    {
        Debug.Log("Animator IK");
        if (IkTargetLeft != null && IkTargetRight != null)
        {
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            anim.SetIKPosition(AvatarIKGoal.RightHand, IkTargetRight.position);
            anim.SetIKRotation(AvatarIKGoal.RightHand, IkTargetRight.rotation);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            anim.SetIKPosition(AvatarIKGoal.LeftHand, IkTargetLeft.position);
            anim.SetIKRotation(AvatarIKGoal.LeftHand, IkTargetLeft.rotation);
        }
    }
}