using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Destructable : MonoBehaviour
{
    [SerializeField] protected float lifepoints = 100f;
    protected float currentLifepoints;
    private PhotonView photonView;

    public PhotonView PhotonView { get => photonView; set => photonView = value; }

    protected void Start()
    {
        PhotonView = GetComponent<PhotonView>();
        currentLifepoints = lifepoints;
    }

    [PunRPC]
    public void InflictDamage(float damage)
    {
        if (!PhotonView.IsMine)
            return;

        currentLifepoints -= damage;
        Debug.Log($"{name} Damage: {damage} Life left: {currentLifepoints}");

        if (currentLifepoints <= 0)
            Destruct();
    }

    public virtual void Destruct()
    {
        Debug.Log("Killed " + gameObject.name);
        Destroy(gameObject);
    }
}
