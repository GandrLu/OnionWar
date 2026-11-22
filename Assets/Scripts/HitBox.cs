using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HitBox : MonoBehaviour
{
    [SerializeField] float defaultDamage;
    private Destructable associatedDestructable;

    void Awake()
    {
        associatedDestructable = GetComponent<Destructable>();
        if (associatedDestructable == null)
        {
            associatedDestructable = GetComponentInParent<Destructable>();
        }
        if (associatedDestructable == null)
            throw new MissingReferenceException();
    }

    public void Hit(Vector3 position, Quaternion rotation, int teamID)
    {
        associatedDestructable.PhotonView.RPC(nameof(associatedDestructable.InflictDamage), RpcTarget.All, new object[2] { defaultDamage, teamID});
        if (associatedDestructable.HitEffect != null)
            associatedDestructable.PhotonView.RPC("PlayHitEffect", RpcTarget.All, new object[2] { position, rotation });
    }
}
