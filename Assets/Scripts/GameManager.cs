using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    #region Public Fields
    public static GameManager Instance;
    public GameObject playerPrefab;
    #endregion
    #region Private Fields
    [SerializeField] ToggleGroup spawnToggleGroup;
    [SerializeField] Button spawnConfirmButton;
    [SerializeField] GameObject spawnCanvas;
    [SerializeField] GameObject hudCanvas;
    private GameObject player;
    private Vector3 spawnPosition;
    private float mapImageScaleFactor = 5.5f;
    private bool isSpawnReady;
    private bool isPlayerDead;
    #endregion

    #region Unity Callbacks
    private void Start()
    {
        Instance = this;
        if (playerPrefab == null)
        {
            Debug.LogError("<Color=Red><a>Missing</a></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
        }
        else
        {
            if (PlayerPhotonManager.LocalPlayerInstance == null)
            {
                Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManager.GetActiveScene().name);
                // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                player = PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(0, -100, 0), Quaternion.identity, 0);
                player.SetActive(false);
            }
            else
            {
                Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
            }

        }
        spawnConfirmButton.onClick.AddListener(SetSpawnReady);
        isPlayerDead = true;
    }

    private void Update()
    {
        spawnCanvas.SetActive(isPlayerDead);
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

        if (isPlayerDead && isSpawnReady)
        {
            SpawnPlayer();
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

        //if (PhotonNetwork.IsMasterClient)
        //{
        //    Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient);

        //    LoadArena();
        //}
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogFormat("OnPlayerLeftRoom(){0}", otherPlayer.NickName);

        //if (PhotonNetwork.IsMasterClient)
        //{
        //    Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient);

        //    LoadArena();
        //}
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
    }
    #endregion

    private void SpawnPlayer()
    { 
        player.transform.position = spawnPosition;
        player.GetPhotonView().RPC("SetActive", RpcTarget.Others);
        player.SetActive(true);
        isPlayerDead = false;
    }

    private void SetSpawnReady()
    {
        if (spawnToggleGroup.AnyTogglesOn())
            isSpawnReady = true;
    }
}
