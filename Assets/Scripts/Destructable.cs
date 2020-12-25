using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Events;

public class Destructable : MonoBehaviour
{
    [SerializeField] protected float lifepoints = 100f;
    [SerializeField] GameObject hitEffect;
    private float currentLifepoints;
    private PhotonView photonView;
    private UnityEvent damageEvent;

    public float CurrentLifepoints { get => currentLifepoints; set => currentLifepoints = value; }
    public GameObject HitEffect { get => hitEffect; set => hitEffect = value; }
    public PhotonView PhotonView { get => photonView; set => photonView = value; }
    public UnityEvent DamageEvent
    {
        get
        {
            if (damageEvent == null)
                damageEvent = new UnityEvent();
            return damageEvent;
        }
        set => damageEvent = value;
    }


    protected void Start()
    {
        PhotonView = GetComponent<PhotonView>();
        CurrentLifepoints = lifepoints;
    }

    [PunRPC]
    public void InflictDamage(float damage)
    {
        if (!PhotonView.IsMine)
            return;

        CurrentLifepoints -= damage;
        DamageEvent.Invoke();
        Debug.Log($"{name} Damage: {damage} Life left: {CurrentLifepoints}");

        if (CurrentLifepoints <= 0)
            photonView.RPC("Destruct", RpcTarget.All);
    }

    [PunRPC]
    public virtual void Destruct()
    {
        Debug.Log("Killed " + gameObject.name);
        Destroy(gameObject);
    }

    public virtual void Resurrect()
    {
        CurrentLifepoints = lifepoints;
    }
}
