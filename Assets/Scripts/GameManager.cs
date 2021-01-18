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
    [SerializeField] ToggleGroup spawnToggleGroup;
    [SerializeField] Button spawnConfirmButton;
    [SerializeField] GameObject spawnCanvas;
    [SerializeField] GameObject hudCanvas;
    [SerializeField] Slider lifepointSlider;
    [SerializeField] Text ammoText;
    [SerializeField] int notShootableLayer;
    [SerializeField] Text spawnText;
    [SerializeField] Image hitImage;
    [SerializeField] Color hitColor;
    #endregion

    #region Private Fields
    private static GameManager instance;
    private GameObject player;
    private PlayerDestructable playerDestructable;
    private PlayerMovement playerMovement;
    private PlayerShooting playerShooting;
    private Vector3 spawnPosition;
    private float mapImageScaleFactor = 5.5f;
    private float hitFlashSpeed = 3f;
    private float spawnTimer;
    private float spawnTime = 5f;
    private int cancelKeyHits;
    private bool isSpawnReady;
    private bool isPlayerDead;
    private bool isHit;
    #endregion

    public Text AmmoText { get => ammoText; set => ammoText = value; }
    public static GameManager Instance { get => instance; }

    #region Unity Callbacks
    private void Awake()
    {
        if (lifepointSlider == null)
            throw new MissingReferenceException();
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
        spawnConfirmButton.onClick.AddListener(SetSpawnReady);
        isPlayerDead = true;
    }

    private void Update()
    {
        spawnCanvas.SetActive(isPlayerDead && spawnTimer <= 0f);
        hudCanvas.SetActive(!isPlayerDead);

        if (isPlayerDead)
        {
            if (spawnToggleGroup.AnyTogglesOn())
            {
                foreach (var toggle in spawnToggleGroup.ActiveToggles())
                {
                    if (toggle.isOn)
                    {
                        var togglePos = toggle.transform.localPosition;
                        spawnPosition = new Vector3(togglePos.x, 0.2f, togglePos.y) / mapImageScaleFactor;
                    }
                }
            }
        }

        if (isPlayerDead && spawnTimer > 0f)
        {
            spawnTimer -= Time.deltaTime;
            spawnText.text = string.Format("{0:0}", spawnTimer);
        }
        else if(isPlayerDead && spawnTimer <= 0f)
        {
            spawnText.enabled = false;
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

        if (isHit)
        {
            hitImage.color = hitColor;
        }
        else
        {
            hitImage.color = Color.Lerp(hitImage.color, Color.clear, hitFlashSpeed * Time.deltaTime);
        }
        isHit = false;
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
    }

    public void TakeHit()
    {
        isHit = true;
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

    private void SpawnPlayer()
    {
        player.transform.position = spawnPosition;
        player.GetPhotonView().RPC(nameof(PlayerDestructable.SetActive), RpcTarget.All);
        lifepointSlider.value = lifepointSlider.maxValue;
        playerDestructable.Resurrect();
        playerMovement.enabled = true;
        playerShooting.enabled = true;
        //player.SetActive(true);
        playerShooting.ReloadWeapon();
        isPlayerDead = false;
        //spawnText.enabled = false;
        spawnTimer = 0f;
    }

    private void SetSpawnReady()
    {
        if (spawnToggleGroup.AnyTogglesOn())
            isSpawnReady = true;
    }

    private void UpdateHudLifepoints()
    {
        lifepointSlider.value = playerDestructable.CurrentLifepoints;
    }
    #endregion
}
