using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Launcher : MonoBehaviourPunCallbacks
{
    #region Private Serializable Fields

    [SerializeField] private byte maxPlayerPerRoom = 4;

    #endregion

    #region Private Fields

    // This clients game version number
    string gameVersion = "01.2021_01";
    
    /// <summary>
    /// Keep track of the current process. Since connection is asynchronous and is based on several callbacks from Photon,
    /// we need to keep track of this to properly adjust the behavior when we receive call back by Photon.
    /// Typically this is used for the OnConnectedToMaster() callback.
    /// </summary>
    bool isConnecting;

    #endregion

    #region Public Fields

    [SerializeField] private GameObject controlPanel;
    [SerializeField] private GameObject progressLabel;
    [SerializeField] private Button quitButton;
    [SerializeField] private Text versionText;

    #endregion

    #region Unity CallBacks

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        if (controlPanel == null)
            throw new MissingReferenceException();
        if (progressLabel == null)
            throw new MissingReferenceException();
        if (quitButton == null)
            throw new MissingReferenceException();
        if (versionText == null)
            throw new MissingReferenceException();
        quitButton.onClick.AddListener(QuitGame);
    }

    private void Start()
    {
        controlPanel.SetActive(true);
        progressLabel.SetActive(false);
        versionText.text = "v" + gameVersion;
        //PhotonNetwork.SendRate = 40; // Default 20
        //PhotonNetwork.SerializationRate = 40; // Default 10
    }

    #endregion

    #region Public Methods

    public void Connect()
    {
        controlPanel.SetActive(false);
        progressLabel.SetActive(true);

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            isConnecting = PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;
        }
    }

    #endregion

    #region MonoBehaviourPunCallbacks Callbacks

    public override void OnConnectedToMaster()
    {
        if (isConnecting)
        {
            Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
            PhotonNetwork.JoinRandomRoom();
            isConnecting = false;
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        controlPanel.SetActive(true);
        progressLabel.SetActive(false);

        Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by " +
            "PUN with reason {0}", cause);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. " +
            "No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayerPerRoom });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            Debug.Log("We load the default level");

            PhotonNetwork.LoadLevel(4);
        }
    }

    #endregion

    #region Private Methods
    private void QuitGame()
    {
        Application.Quit();
    }
    #endregion
}
