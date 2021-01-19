using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuCursorTarget : MonoBehaviour
{
    void Update()
    {
        var screenPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 3f);
        transform.position = Camera.main.ScreenToWorldPoint(screenPosition);
    }
}
