using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    [SerializeField]
    private float m_CameraOffsetZ = 7.5f;
    [SerializeField]
	private float smoothing = 5f;
    [SerializeField]
	private Transform target;
    private bool is_FollowActive;
    private int m_ScreenWidth = 0;

	Vector3 offset;

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
        if (!is_FollowActive)
            return;
        Initialize();
	}

	void FixedUpdate()
	{
        if (!is_FollowActive)
            return;
        Vector3 targetCamPos = Target.position + offset;
		transform.position = Vector3.Lerp (transform.position, targetCamPos, smoothing + Time.fixedDeltaTime);

        //if (Input.GetMouseButton(2))
        //{
        //    Debug.Log(Input.mousePosition);
        //    if (Input.mousePosition.x < m_ScreenWidth / 2)
        //    {
        //        this.transform.RotateAround(target.position, Vector3.up, -5f);
        //        Debug.Log("rotate+");
        //    }
        //    else
        //    {
        //        this.transform.RotateAround(target.position, Vector3.up, 5f);
        //        Debug.Log("rotate-");
        //    }
        //}
        //offset = transform.position - target.position;
        
        //transform.RotateAround(target.position, Vector3.up, target.rotation.eulerAngles.y);
        transform.LookAt(Target);
	}

    private void Initialize()
    {
        transform.position = new Vector3(Target.position.x, transform.position.y, Target.position.z - m_CameraOffsetZ);
        transform.rotation.Set(0, Target.rotation.y, 0, 0);
        m_ScreenWidth = Screen.width;
        offset = transform.position - Target.position;
	}
}
