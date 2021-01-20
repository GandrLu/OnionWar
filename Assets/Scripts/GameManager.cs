using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public sealed class GameManager : MonoBehaviourPunCallbacks
{
    #region Serialized Fields
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject togglePrefab;
    [SerializeField] GameObject spawnPointRoot;
    [SerializeField] GameObject spawnCanvas;
    [SerializeField] GameObject hudCanvas;
    [SerializeField] Camera mapViewCamera;
    [SerializeField] Color hitColor;
    [SerializeField] ToggleGroup spawnToggleGroup;
    [SerializeField] Button spawnConfirmButton;
    [SerializeField] Slider lifepointSlider;
    [SerializeField] Text ammoText;
    [SerializeField] Text spawnText;
    [SerializeField] Image itemImage;
    [SerializeField] Image hitImage;
    [SerializeField] int notShootableLayer;
    #endregion

    #region Private Fields
    private static GameManager instance;
    private Dictionary<Toggle, Vector3> spawnTogglePositionPairs = new Dictionary<Toggle, Vector3>();
    private GameObject player;
    private Camera mainCamera;
    private PlayerDestructable playerDestructable;
    private PlayerMovement playerMovement;
    private PlayerShooting playerShooting;
    private Vector3 spawnPosition;
    private float mapImageScaleFactor = 5.5f;
    private float hitFlashSpeed = 1f;
    private float spawnTimer;
    private float spawnTime = 5f;
    private int cancelKeyHits;
    private bool isSpawnReady;
    private bool isPlayerDead;
    #endregion

    #region Properties
    public Text AmmoText { get => ammoText; set => ammoText = value; }
    public static GameManager Instance { get => instance; }
    public Image ItemImage { get => itemImage; set => itemImage = value; }
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        if (lifepointSlider == null)
            throw new MissingReferenceException();
        if (mapViewCamera == null)
            throw new MissingReferenceException();
        if (togglePrefab == null)
            throw new MissingReferenceException();
        if (spawnPointRoot == null)
            throw new MissingReferenceException();
        if (spawnCanvas == null)
            throw new MissingReferenceException();
        if (hudCanvas == null)
            throw new MissingReferenceException();
        if (spawnToggleGroup == null)
            throw new MissingReferenceException();
        if (spawnConfirmButton == null)
            throw new MissingReferenceException();
        if (lifepointSlider == null)
            throw new MissingReferenceException();
        if (ammoText == null)
            throw new MissingReferenceException();
        if (spawnText == null)
            throw new MissingReferenceException();
        if (itemImage == null)
            throw new MissingReferenceException();
        if (hitImage == null)
            throw new MissingReferenceException();

        mainCamera = Camera.main;
    }

    private void Start()
    {
        instance = this;
        if (playerPrefab == null)
        {
            Debug.LogError("<Color=Red><a>Missing</a></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
        }
        else
        {
            InstantiatePlayer();
        }
        mainCamera.gameObject.SetActive(false);
        mapViewCamera.gameObject.SetActive(true);
        spawnConfirmButton.onClick.AddListener(SetSpawnReady);
        isPlayerDead = true;

        var spawnPoints = new List<Vector3>();
        foreach (Transform child in spawnPointRoot.transform)
            spawnPoints.Add(child.position);

        foreach (var sp in spawnPoints)
        {
            var toggleObj = Instantiate(togglePrefab, spawnToggleGroup.transform);
            var toggle = toggleObj.GetComponent<Toggle>();
            var screenPos = mapViewCamera.WorldToScreenPoint(sp);
            toggle.GetComponent<RectTransform>().anchoredPosition = screenPos;
            toggle.group = spawnToggleGroup;
            spawnToggleGroup.RegisterToggle(toggle);
            spawnTogglePositionPairs.Add(toggle, sp);
        }
        spawnToggleGroup.EnsureValidState();
    }

    private void Update()
    {
        if (isPlayerDead)
        {
            if (spawnToggleGroup.AnyTogglesOn())
            {
                foreach (var toggle in spawnToggleGroup.ActiveToggles())
                {
                    if (toggle.isOn)
                    {
                        spawnPosition = spawnTogglePositionPairs[toggle];
                    }
                }
            }
        }

        if (isPlayerDead && spawnTimer > 0f)
        {
            spawnTimer -= Time.deltaTime;
            spawnText.text = string.Format("{0:0}", spawnTimer);
        }
        else if (isPlayerDead && spawnTimer <= 0f)
        {
            ShowSpawnScreen();
        }

        if (isPlayerDead && isSpawnReady && spawnTimer <= 0f)
        {
            SpawnPlayer();
        }

        if (Input.GetButtonDown("Cancel"))
        {
            if (++cancelKeyHits >= 2)
                LeaveRoom();
        }
    }
    #endregion

    #region Photon Callbacks
    /// <summary>
    /// Called when the local player left the room. We need to load the launcher scene.
    /// </summary>
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }

    public override void OnPlayerEnteredRoom(Player other)
    {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName);
        // Equip correct weapon at joined players instance of this player
        playerShooting.photonView.RPC(nameof(playerShooting.ChangeWeapon), other, playerShooting.ActiveWeaponIndex);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogFormat("OnPlayerLeftRoom(){0}", otherPlayer.NickName);
    }
    #endregion

    #region Public Methods
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void SetPlayerDead()
    {
        isPlayerDead = true;
        isSpawnReady = false;
        playerMovement.enabled = false;
        playerShooting.enabled = false;
        spawnTimer += spawnTime;
        spawnText.enabled = true;
        hudCanvas.SetActive(false);
    }

    public void TakeHit()
    {
        StartCoroutine(LerpHitColor());
    }
    #endregion

    #region Private Methods
    private void InstantiatePlayer()
    {
        if (PlayerPhotonManager.LocalPlayerInstance == null)
        {
            Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManager.GetActiveScene().name);
            // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
            player = PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity, 0);
            player.SetActive(false);
            // Set layer of own hitboxes to not shootable to avoid shooting yourself
            foreach (var hitbox in player.GetComponentsInChildren<HitBox>())
                hitbox.gameObject.layer = notShootableLayer;
            playerDestructable = player.GetComponent<PlayerDestructable>();
            playerDestructable.DamageEvent.AddListener(UpdateHudLifepoints);
            playerDestructable.DamageEvent.AddListener(TakeHit);
            playerMovement = player.GetComponent<PlayerMovement>();
            playerShooting = player.GetComponent<PlayerShooting>();
        }
        else
        {
            Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
        }
    }

    private IEnumerator LerpHitColor()
    {
        float elapsedTime = 0f;
        while (elapsedTime < hitFlashSpeed)
        {
            hitImage.color = Color.Lerp(hitColor, Color.clear, elapsedTime / hitFlashSpeed);
            elapsedTime += Time.deltaTime;

            yield return null;
        }
        hitImage.color = Color.clear;
    }

    private void SetSpawnReady()
    {
        if (spawnToggleGroup.AnyTogglesOn())
            isSpawnReady = true;
    }

    private void ShowSpawnScreen()
    {
        spawnCanvas.SetActive(true);
        spawnText.enabled = false;
        mainCamera.gameObject.SetActive(false);
        mapViewCamera.gameObject.SetActive(true);
    }

    private void SpawnPlayer()
    {
        player.transform.position = spawnPosition;
        player.GetPhotonView().RPC(nameof(PlayerDestructable.SetActive), RpcTarget.All);
        lifepointSlider.value = lifepointSlider.maxValue;
        playerDestructable.Resurrect();
        playerMovement.enabled = true;
        playerShooting.enabled = true;
        playerShooting.ReloadWeapon();
        isPlayerDead = false;
        spawnTimer = 0f;
        mainCamera.gameObject.SetActive(true);
        mapViewCamera.gameObject.SetActive(false);
        spawnCanvas.SetActive(false);
        hudCanvas.SetActive(true);
    }

    private void UpdateHudLifepoints()
    {
        lifepointSlider.value = playerDestructable.CurrentLifepoints;
    }
    #endregion
}
