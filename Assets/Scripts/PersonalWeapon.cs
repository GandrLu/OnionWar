using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PersonalWeaponType
{
    None, Rifle, Pistol, Launcher, Melee
}

public class PersonalWeapon : MonoBehaviour
{
    [SerializeField]
    private Vector3 aimingPosition;
    [SerializeField]
    private Vector3 aimingRotation;
    [SerializeField]
    private Vector3 holdingPosition;
    [SerializeField]
    private Vector3 holdingRotation;
    [SerializeField]
    private PersonalWeaponType weaponType;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetHoldingTransform()
    {
        transform.localPosition = holdingPosition;
        transform.localRotation = Quaternion.Euler(holdingRotation);
    }
    
    public void SetAimingTransform()
    {
        transform.localPosition = aimingPosition;
        transform.localRotation = Quaternion.Euler(aimingRotation);
    }

    public PersonalWeaponType GetWeaponType()
    {
        return weaponType;
    }
}
