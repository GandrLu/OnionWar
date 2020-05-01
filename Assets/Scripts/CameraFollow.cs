using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	public Transform target;
	public float smoothing = 5f;
    int m_ScreenWidth = 0;
    float m_CameraOffsetZ = 7.5f;

	Vector3 offset;

	void Start()
	{
        transform.position = new Vector3( target.position.x, transform.position.y, target.position.z - m_CameraOffsetZ);
        transform.rotation.Set(0, target.rotation.y, 0, 0);
        m_ScreenWidth = Screen.width;
		offset = transform.position - target.position;
	}

	void FixedUpdate()
	{
        Vector3 targetCamPos = target.position + offset;
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
        transform.RotateAround(target.position, Vector3.up, target.rotation.eulerAngles.y);
        transform.LookAt(target);
	}
}
