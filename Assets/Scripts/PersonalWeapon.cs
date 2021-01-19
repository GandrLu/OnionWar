using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PersonalWeaponType
{
    None, Rifle, Pistol, Launcher, Melee
}

public class PersonalWeapon : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] Transform refLeft;
    [SerializeField] Transform refRight;
    [SerializeField] PersonalWeaponType weaponType;
    [SerializeField] float accuracy;
    [SerializeField] float reloadTime;
    [SerializeField] float recoil;
    [SerializeField] float timeBetweenShots;
    [SerializeField] int bulletChamberSize;
    [SerializeField] Sprite hudImage;
    #endregion

    #region Private Fields
    private ParticleSystem muzzleFlash;
    private LineRenderer aimingLine;
    private Transform shootingPosition;
    private int loadedBullets;
    #endregion

    #region Properties
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
    public Transform RefLeft { get => refLeft; set => refLeft = value; }
    public Transform RefRight { get => refRight; set => refRight = value; }
    public Sprite HudImage { get => hudImage; set => hudImage = value; }
    #endregion

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
    #endregion
}
