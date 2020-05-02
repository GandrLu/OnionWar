using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private string hitTag = "Enemy";
    [SerializeField]
    private float speed = 20f;
    void Start()
    {
        
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    //private void OnCollisionEnter(Collision other)
    private void OnTriggerEnter(UnityEngine.Collider other)
    {
        if (other.transform.tag == hitTag)
        { 
            Debug.Log("Hit " + other.transform.name);
            Destroy(gameObject);
        }
    }
}
