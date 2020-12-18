using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HitBox : MonoBehaviour
{
    [SerializeField] string damagingTag = "Damaging";
    [SerializeField] float defaultDamage;
    private Destructable associatedDestructable;
    private bool isHit;

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

    public void Hit(Vector3 position, Quaternion rotation)
    {
        associatedDestructable.PhotonView.RPC("InflictDamage", RpcTarget.All, defaultDamage);
        if (associatedDestructable.HitEffect != null)
            associatedDestructable.PhotonView.RPC("PlayHitEffect", RpcTarget.All, new object[2] { position, rotation });
    }
}
