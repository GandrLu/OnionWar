using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDestructable : Destructable
{
    PlayerMovement playerMovement;
    PlayerShooting playerShooting;

    protected new void Start()
    {
        base.Start();
        playerMovement = GetComponent<PlayerMovement>();
        playerShooting = GetComponent<PlayerShooting>();
    }

    [PunRPC]
    public override void Destruct()
    {
        gameObject.SetActive(false);
        if (PhotonView.IsMine)
        {
            Debug.Log("Killed " + PhotonView.Owner.NickName);
            //playerMovement.enabled = false;
            //playerShooting.enabled = false;
            //GameManager.Instance.LeaveRoom();
            GameManager.Instance.SetPlayerDead();
        }
    }

    [PunRPC]
    public void SetActive()
    {
        gameObject.SetActive(true);
    }
}
