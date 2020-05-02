using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collider : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnCollisionEnter(Collision other)
    {
        Debug.Log("Collision " + other.transform.name);
    }
    private void OnTriggerEnter(UnityEngine.Collider other)
    {
        Debug.Log("Triggered " + other.transform.name);
    }
}
