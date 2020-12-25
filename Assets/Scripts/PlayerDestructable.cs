using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDestructable : Destructable
{
    [SerializeField] bool ragdollActive;
    [SerializeField] bool ragdollInactive;
    private Collider[] ragdollColliders = new Collider[0];
    private Collider hitboxCollider;

    protected new void Start()
    {
        base.Start();
        //ragdollColliders = GetComponentsInChildren<Collider>();
        //var colliderList = new List<Collider>(ragdollColliders);
        //var mainCollider = GetComponentInChildren<HitBox>().GetComponent<Collider>();
        //var aimingPlane = transform.Find("AimingPlane").GetComponent<Collider>();
        //colliderList.Remove(mainCollider);
        //colliderList.Remove(aimingPlane);
        //ragdollColliders = colliderList.ToArray();
        //hitboxCollider = mainCollider;
        //EnableRagdoll(false);
    }

    private void Update()
    {
        if (ragdollActive)
            EnableRagdoll(ragdollActive);
        if (ragdollInactive)
            EnableRagdoll(!ragdollInactive);
    }

    public void EnableRagdoll(bool shouldEnable)
    {
        //if (hitboxCollider == null)
        //    return;
        //hitboxCollider.enabled = !shouldEnable;
        //foreach (var collider in ragdollColliders)
        //    collider.enabled = shouldEnable;
        
        var rigidbody = GetComponent<Rigidbody>();
        rigidbody.useGravity = !shouldEnable;
        rigidbody.isKinematic = shouldEnable;
        GetComponent<Animator>().enabled = !shouldEnable;
    }

    [PunRPC]
    public override void Destruct()
    {
        //gameObject.SetActive(false);
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
        EnableRagdoll(false);
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
