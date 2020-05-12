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
    private Quaternion aimingRotation;
    [SerializeField]
    private Vector3 holdingPosition;
    [SerializeField]
    private Quaternion holdingRotation;
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
        transform.localRotation = holdingRotation;
    }
    
    public void SetAimingTransform()
    {
        transform.localPosition = aimingPosition;
        transform.localRotation = aimingRotation;
    }

    public PersonalWeaponType GetWeaponType()
    {
        return weaponType;
    }
}
