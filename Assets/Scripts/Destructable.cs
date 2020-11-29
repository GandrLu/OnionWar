using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructable : MonoBehaviour
{
    [SerializeField] protected float lifepoints = 100f;
    protected float currentLifepoints;
    
    // Start is called before the first frame update
    void Start()
    {
        currentLifepoints = lifepoints;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InflictDamage(float damage)
    {
        currentLifepoints -= damage;
        if (currentLifepoints <= 0)
            Destruct();
    }

    public virtual void Destruct()
    {
        Destroy(gameObject);
    }
}
