using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDestructable : Destructable
{
    private Collider[] ragdollColliders = new Collider[0];
    private List<Collider> hitboxColliders = new List<Collider>();
    private Rigidbody[] ragdollRigidbodies = new Rigidbody[0];
    private Rigidbody mainRigidbody;

    protected new void Start()
    {
        base.Start();
        mainRigidbody = GetComponent<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        var rdColliderList = new List<Collider>(ragdollColliders);
        
        var rigidbodyList = new List<Rigidbody>(ragdollRigidbodies);
        rigidbodyList.Remove(mainRigidbody);
        ragdollRigidbodies = rigidbodyList.ToArray();

        var hitboxes = GetComponentsInChildren<HitBox>();
        foreach (var hb in hitboxes)
        {
            var hbCollider = hb.GetComponent<Collider>();
            hitboxColliders.Add(hbCollider);
            rdColliderList.Remove(hbCollider);
        }
        var aimingPlane = transform.Find("AimingPlane").GetComponent<Collider>();
        rdColliderList.Remove(aimingPlane);
        ragdollColliders = rdColliderList.ToArray();

        EnableRagdoll(false);
    }

    public void EnableRagdoll(bool shouldEnable)
    {
        if (hitboxColliders.Count <= 0)
            return;

        foreach (var hbCollider in hitboxColliders)
            hbCollider.enabled = !shouldEnable;
        
        foreach (var collider in ragdollColliders)
            collider.enabled = shouldEnable;

        foreach (var rb in ragdollRigidbodies)
        {
            rb.useGravity = shouldEnable;
            rb.isKinematic = !shouldEnable;
        }

        mainRigidbody.useGravity = !shouldEnable;
        GetComponent<Animator>().enabled = !shouldEnable;
    }

    [PunRPC]
    public override void Destruct()
    {
        EnableRagdoll(true);
        if (PhotonView.IsMine)
        {
            Debug.Log("Killed " + PhotonView.Owner.NickName);
            GameManager.Instance.SetPlayerDead();
        }
    }

    public override void Resurrect()
    {
        base.Resurrect();
    }

    [PunRPC]
    public void SetActive()
    {
        EnableRagdoll(false);
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
