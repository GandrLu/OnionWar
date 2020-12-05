using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PersonalWeaponType
{
    None, Rifle, Pistol, Launcher, Melee
}

public class PersonalWeapon : MonoBehaviour
{
    [SerializeField] Vector3 aimingPosition;
    [SerializeField] Vector3 aimingRotation;
    [SerializeField] Vector3 holdingPosition;
    [SerializeField] Vector3 holdingRotation;
    [SerializeField] PersonalWeaponType weaponType;
    private LineRenderer aimingLine;
    private ParticleSystem muzzleFlash;
    private Transform shootingPosition;

    public LineRenderer AimingLine { get => aimingLine; set => aimingLine = value; }
    public ParticleSystem MuzzleFlash { get => muzzleFlash; set => muzzleFlash = value; }
    public Transform ShootingPosition { get => shootingPosition; set => shootingPosition = value; }
    public PersonalWeaponType WeaponType { get => weaponType; set => weaponType = value; }

    private void Awake()
    {
        AimingLine = GetComponentInChildren<LineRenderer>();
        if (AimingLine == null)
            throw new MissingReferenceException("Aiming line renderer missing!");
        MuzzleFlash = GetComponentInChildren<ParticleSystem>();
        if (MuzzleFlash == null)
            throw new MissingReferenceException("Muzzle flash particle system missing!");
        ShootingPosition = AimingLine.transform;
    }

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
}
