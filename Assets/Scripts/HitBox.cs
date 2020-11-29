using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] string damagingTag = "Damaging";
    [SerializeField] float defaultDamage;
    private Destructable associatedDestructable;

    void Start()
    {
        associatedDestructable = GetComponentInParent<Destructable>();
        if (associatedDestructable == null)
            throw new MissingReferenceException();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == damagingTag)
        {
            associatedDestructable.InflictDamage(defaultDamage);
        }
    }
}
