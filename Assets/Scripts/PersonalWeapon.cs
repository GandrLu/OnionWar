using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PersonalWeaponType
{
    None, Rifle, Pistol, Launcher, Melee
}

public class PersonalWeapon : MonoBehaviour
{
    // Position fields
    [SerializeField] Vector3 aimingPosition;
    [SerializeField] Vector3 aimingRotation;
    [SerializeField] Vector3 holdingPosition;
    [SerializeField] Vector3 holdingRotation;

    [SerializeField] PersonalWeaponType weaponType;
    [SerializeField] float accuracy;
    [SerializeField] float reloadTime;
    [SerializeField] float recoil;
    [SerializeField] float timeBetweenShots;
    [SerializeField] int bulletChamberSize;
    private int loadedBullets;
    private LineRenderer aimingLine;
    private ParticleSystem muzzleFlash;
    private Transform shootingPosition;

    public LineRenderer AimingLine { get => aimingLine; set => aimingLine = value; }
    public ParticleSystem MuzzleFlash { get => muzzleFlash; set => muzzleFlash = value; }
    public Transform ShootingPosition { get => shootingPosition; set => shootingPosition = value; }
    public PersonalWeaponType WeaponType { get => weaponType; set => weaponType = value; }
    public float TimeBetweenShots { get => timeBetweenShots; set => timeBetweenShots = value; }
    public int LoadedBullets { get => loadedBullets; set => loadedBullets = value; }
    public int BulletChamberSize { get => bulletChamberSize; set => bulletChamberSize = value; }
    public float ReloadTime { get => reloadTime; set => reloadTime = value; }
    public float Accuracy { get => accuracy; set => accuracy = value; }
    public float Recoil { get => recoil; set => recoil = value; }

    #region Unity Callbacks
    private void Awake()
    {
        AimingLine = GetComponentInChildren<LineRenderer>();
        if (AimingLine == null)
            throw new MissingReferenceException("Aiming line renderer missing!");
        MuzzleFlash = GetComponentInChildren<ParticleSystem>();
        if (MuzzleFlash == null)
            throw new MissingReferenceException("Muzzle flash particle system missing!");
        ShootingPosition = AimingLine.transform;
        LoadedBullets = BulletChamberSize;
    }

    private void Start()
    {
    }

    private void Update()
    {
    }
    #endregion

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
