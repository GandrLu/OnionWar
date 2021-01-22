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
    [SerializeField] Sprite hudImage;
    [SerializeField] AudioClip audioClip;
    [SerializeField] PersonalWeaponType weaponType;
    [SerializeField] float accuracy;
    [SerializeField] float reloadTime;
    [SerializeField] float recoil;
    [SerializeField] float timeBetweenShots;
    [SerializeField] int bulletChamberSize;
    [SerializeField] int range;
    [SerializeField] bool hasAutomaticFire;
    #endregion

    #region Private Fields
    private ParticleSystem muzzleFlash;
    private LineRenderer aimingLine1;
    private LineRenderer aimingLine2;
    private LineRenderer firingLine;
    private Transform shootingPosition;
    private float firingLineTimer;
    private float firingLineTime = 0.1f;
    private int loadedBullets;
    #endregion

    #region Properties
    public LineRenderer AimingLine1 { get => aimingLine1; set => aimingLine1 = value; }
    public LineRenderer AimingLine2 { get => aimingLine2; set => aimingLine2 = value; }
    public ParticleSystem MuzzleFlash { get => muzzleFlash; set => muzzleFlash = value; }
    public Transform ShootingPosition { get => shootingPosition; set => shootingPosition = value; }
    public PersonalWeaponType WeaponType { get => weaponType; set => weaponType = value; }
    public float TimeBetweenShots { get => timeBetweenShots; set => timeBetweenShots = value; }
    public int LoadedBullets { get => loadedBullets; set => loadedBullets = value; }
    public int BulletChamberSize { get => bulletChamberSize; set => bulletChamberSize = value; }
    public float ReloadTime { get => reloadTime; set => reloadTime = value; }
    public float Accuracy { get => accuracy; set => accuracy = value; }
    public int Range { get => range; set => range = value; }
    public float Recoil { get => recoil; set => recoil = value; }
    public Transform RefLeft { get => refLeft; set => refLeft = value; }
    public Transform RefRight { get => refRight; set => refRight = value; }
    public Sprite HudImage { get => hudImage; set => hudImage = value; }
    public bool HasAutomaticFire { get => hasAutomaticFire; set => hasAutomaticFire = value; }
    public AudioClip AudioClip { get => audioClip; set => audioClip = value; }
    public LineRenderer FiringLine { get => firingLine; set => firingLine = value; }
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>();
        if (lineRenderers.Length >= 3)
        {
            AimingLine1 = lineRenderers[0];
            AimingLine2 = lineRenderers[1];
            FiringLine = lineRenderers[2];
        }
        if (AimingLine1 == null)
            throw new MissingReferenceException("Aiming line 1 renderer missing!");
        if (AimingLine2 == null)
            throw new MissingReferenceException("Aiming line 2 renderer missing!");
        if (FiringLine == null)
            throw new MissingReferenceException("Firing line renderer missing!");
        MuzzleFlash = GetComponentInChildren<ParticleSystem>();
        if (MuzzleFlash == null)
            throw new MissingReferenceException("Muzzle flash particle system missing!");
        ShootingPosition = AimingLine1.transform;
        LoadedBullets = BulletChamberSize;
    }

    private void Update()
    {
        if (firingLineTimer <= 0f)
            FiringLine.enabled = false;
        else
            firingLineTimer -= Time.deltaTime;
    }
    #endregion

    #region Public Methods
    public void FireGun(Vector3 pos1, Vector3 pos2)
    {
        firingLineTimer = firingLineTime;
        FiringLine.enabled = true;
        FiringLine.SetPosition(0, pos1);
        FiringLine.SetPosition(1, pos2);
    }
    #endregion
}
