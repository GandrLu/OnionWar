using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;

public class RoundManager : MonoBehaviour
{
    [SerializeField] Text timerText;
    [SerializeField] float roundTime = 600f;
    private double roundTimer;
    private double serverStartTime;
    private const byte RoundEndedEvent = 1;
    private bool hasRoundEnded;

    #region Photon Callbacks
    private void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        if (eventCode == RoundEndedEvent)
        {
            Debug.Log("Received round ended event.");
            GameManager.Instance.LeaveRoom();
        }
    }
    #endregion

    #region Unity Callbacks
    public void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    public void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            serverStartTime = PhotonNetwork.Time;
            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
            customProperties.Add("ServerStartTime", serverStartTime);
            PhotonNetwork.CurrentRoom.SetCustomProperties(customProperties);
        }
        else
            serverStartTime = (double)PhotonNetwork.CurrentRoom.CustomProperties["ServerStartTime"];
    }

    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (roundTimer >= roundTime && !hasRoundEnded)
            {
                RaiseRoundEndedEvent();
                hasRoundEnded = true;
            }
        }

        roundTimer = PhotonNetwork.Time - serverStartTime;
        var remainingTime = roundTime - roundTimer;
        TimeSpan t = TimeSpan.FromSeconds(remainingTime);
        if (!hasRoundEnded)
            timerText.text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);

    }
    #endregion

    #region Private Methods
    private void RaiseRoundEndedEvent()
    {
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(RoundEndedEvent, null, raiseEventOptions, SendOptions.SendReliable);
    }
    #endregion
}
