using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDestructable : Destructable
{
    protected new void Start()
    {
        base.Start();
    }

    [PunRPC]
    public override void Destruct()
    {
        gameObject.SetActive(false);
        if (PhotonView.IsMine)
        {
            Debug.Log("Killed " + PhotonView.Owner.NickName);
            GameManager.Instance.SetPlayerDead();
        }
    }

    [PunRPC]
    public void SetActive()
    {
        gameObject.SetActive(true);
    }

    [PunRPC]
    public void PlayHitEffect(object[] parameters)
    {
        if (parameters.Length < 2)
            return;
        var effect = Instantiate(HitEffect, (Vector3)parameters[0], (Quaternion)parameters[1]);
        Destroy(effect, 0.5f);
    }
}
