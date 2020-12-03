using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    #region Public Fields
    public static GameManager Instance;
    public GameObject playerPrefab;
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
                PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(0f, 0.1f, 0f), Quaternion.identity, 0);
            }
            else
            {
                Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
            }
        }
        PhotonNetwork.InstantiateSceneObject("Target", new Vector3(10f, 0.1f, 10f), Quaternion.identity, 0);
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
    #endregion
}
