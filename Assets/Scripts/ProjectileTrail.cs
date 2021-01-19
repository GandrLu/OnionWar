using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileTrail : MonoBehaviour
{
    [SerializeField] float maxLifetime = 0.5f;
    [SerializeField] float movementSpeed = 200f;
    private float lifetimer;
    private PhotonView photonView;

    private void Start()
    {
        photonView = GetComponent<PhotonView>();
    }

    void Update()
    {
        transform.position = transform.position + transform.forward * movementSpeed * Time.deltaTime;

        lifetimer += Time.deltaTime;
        if (lifetimer >= maxLifetime)
            if (photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
    }
}
