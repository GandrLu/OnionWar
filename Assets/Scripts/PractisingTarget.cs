using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PractisingTarget : Destructable, IPunObservable
{
    [SerializeField] GameObject targetElement;
    private float reactivationTime = 3f;
    private float reactivateTimer;
    private bool isTargetActive;

    new void Start()
    {
        base.Start();
        if (targetElement == null)
            throw new MissingReferenceException();
        ActivateTarget(true);
    }

    void Update()
    {
        if (!isTargetActive)
        {
            reactivateTimer -= Time.deltaTime;
            if (reactivateTimer <= 0)
            {
                ActivateTarget(true);
            }
        }
    }

    public override void Destruct()
    {
        currentLifepoints = lifepoints;
        ActivateTarget(false);
    }

    private void ActivateTarget(bool activate)
    {
        isTargetActive = activate;
        targetElement.SetActive(activate);
        if (activate)
            reactivateTimer = reactivationTime;
    }

    public new void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isTargetActive);
        }
        else if (stream.IsReading)
        {
            var active = (bool)stream.ReceiveNext();
            targetElement.SetActive(active);
        }
    }
}
