using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float m_Speed = 6f;            // The speed that the player will move at.

    //Vector3 movement;                   // The vector to store the direction of the player's movement.
    private Animator anim;                      // Reference to the animator component.
    private Rigidbody playerRigidbody;          // Reference to the player's rigidbody.
    private int floorMask;                      // A layer mask so that a ray can be cast just at gameobjects on the floor layer.
    private float camRayLength = 100f;          // The length of the ray from the camera into the scene.
    //private bool isAiming = false;
    private bool m_IsSprinting = false;
    private int m_ScreenWidth = 0;
    private float h;
    private float v;
    private bool isAiming;

    void Awake()
    {
        // Create a layer mask for the floor layer.
        floorMask = LayerMask.GetMask("Floor");

        // Set up references.
        anim = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();

        m_ScreenWidth = Screen.width;
    }

    void Update(){
        // Store the input axes.
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        isAiming = Input.GetButton("Fire2");
        m_IsSprinting = Input.GetButton("Sprint");
    }


    void FixedUpdate()
    {
        // Move the player around the scene.
        Move(h, v, isAiming);

        // Turn the player to face the mouse cursor.
        Turning(h, v, true);

        // Animate the player.
        Animating(h, v, isAiming);

    }

    void Move(float h, float v, bool aiming)
    {
        float horizontalMovement = h * m_Speed * Time.deltaTime;
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
            transform.localPosition += transform.right * horizontalMovement;
        }
    }

    void Turning(float h, float v, bool aiming)
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
            if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
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
        bool walking = v != 0f || h != 0f;

        // Tell the animator whether or not the player is walking.
        anim.SetBool("IsWalking", walking);
        anim.SetBool("IsAiming", aiming);
        anim.SetFloat("vertical", v);
        anim.SetFloat("horizontal", h);
    }
}