using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public float speed = 6f;            // The speed that the player will move at.

	Vector3 movement;                   // The vector to store the direction of the player's movement.
	Animator anim;                      // Reference to the animator component.
	Rigidbody playerRigidbody;          // Reference to the player's rigidbody.
	int floorMask;                      // A layer mask so that a ray can be cast just at gameobjects on the floor layer.
	float camRayLength = 100f;          // The length of the ray from the camera into the scene.
    //bool isAiming = false;

	void Awake ()
	{
		// Create a layer mask for the floor layer.
		floorMask = LayerMask.GetMask ("Floor");

		// Set up references.
		anim = GetComponent <Animator> ();
		playerRigidbody = GetComponent <Rigidbody> ();
	}


	void FixedUpdate ()
	{
		// Store the input axes.
		float h = Input.GetAxisRaw ("Horizontal");
		float v = Input.GetAxisRaw ("Vertical");

        bool isAiming = Input.GetButton("Fire2");

		// Move the player around the scene.
		Move (h, v, isAiming);

		// Turn the player to face the mouse cursor.
		Turning (h, v, isAiming);

		// Animate the player.
		Animating (h, v, isAiming);

	}

	void Move (float h, float v, bool aiming)
	{
        float horizontalMovement = h * speed * Time.deltaTime;
        float verticalMovement = v * speed * Time.deltaTime;

        if (aiming)
        {
            // Set the movement vector based on the axis input.
            movement.Set(h, 0f, v);

            // Normalize the movement vector and make it proportional to the speed per second.
            movement = movement.normalized * speed * Time.deltaTime;
            //playerRigidbody.AddRelativeForce(speed * h, 0, speed * v)

            // Move the player to it's current position plus the movement.
            //playerRigidbody.MovePosition (new Vector3(transform.localPosition.x * horizontalMovement, 0, transform.localPosition.z * verticalMovement));
            transform.localPosition += transform.forward * verticalMovement;
            transform.localPosition += transform.right * horizontalMovement;
        }
        else
        {
            transform.position = new Vector3(transform.position.x - verticalMovement, 0, transform.position.z + horizontalMovement);
        }
    }

	void Turning (float h, float v, bool aiming)
	{
        if (aiming)
        {
		// Create a ray from the mouse cursor on screen in the direction of the camera.
		Ray camRay = Camera.main.ScreenPointToRay (Input.mousePosition);

		// Create a RaycastHit variable to store information about what was hit by the ray.
		RaycastHit floorHit;

		// Perform the raycast and if it hits something on the floor layer...
		if(Physics.Raycast (camRay, out floorHit, camRayLength, floorMask))
		{
			// Create a vector from the player to the point on the floor the raycast from the mouse hit.
			Vector3 playerToMouse = floorHit.point - transform.position;

			// Ensure the vector is entirely along the floor plane.
			playerToMouse.y = 0f;

			// Create a quaternion (rotation) based on looking down the vector from the player to the mouse.
			Quaternion newRotation = Quaternion.LookRotation (playerToMouse);

			// Set the player's rotation to this new rotation.
			playerRigidbody.MoveRotation (newRotation);
		}
        }
        else
        {
            if(Input.GetButton("Horizontal") || Input.GetButton("Vertical"))
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(-v, 0, h));
            }
        }
	}

	void Animating (float h, float v, bool aiming)
	{
		// Create a boolean that is true if either of the input axes is non-zero.
		bool walking = v != 0f || h != 0f;

		// Tell the animator whether or not the player is walking.
		anim.SetBool ("IsWalking", walking);
        anim.SetBool ("IsAiming", aiming);
        anim.SetFloat ("vertical", v);
        anim.SetFloat ("horizontal", h);
    }
}