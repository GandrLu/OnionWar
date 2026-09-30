using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;
using TMPro;

public class RoundManager : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] Text timerText;
    [SerializeField] float roundTime = 600f;
    [SerializeField] GameMode gameMode;
    [SerializeField] GameObject roundEndPanel;
    [SerializeField] TMP_Text roundEndText;
    [Tooltip("Team names used for the winner announcement, index is the team ID")]
    [SerializeField] string[] teamNames = { "Team A", "Team B" };
    [Tooltip("Seconds the round result is shown before leaving the room")]
    [SerializeField] float leaveRoomDelay = 10f;
    #endregion

    #region Private Fields
    private double roundTimer;
    private double serverStartTime;
    private bool hasRoundEnded;
    #endregion

    #region Photon Callbacks
    private void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        if (eventCode == PhotonEventCodes.RoundEnded)
        {
            Debug.Log("Received round ended event.");
            OnRoundEnded((int)photonEvent.CustomData);
        }
        else if (eventCode == PhotonEventCodes.PlayerKilled && PhotonNetwork.IsMasterClient && !hasRoundEnded)
        {
            var data = (int[])photonEvent.CustomData;
            gameMode.OnPlayerKilled(data[0], data[1]);
        }
    }
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        if (timerText == null)
            throw new MissingReferenceException();
        if (gameMode == null)
            throw new MissingReferenceException();
        if (roundEndPanel == null)
            throw new MissingReferenceException();
        if (roundEndText == null)
            throw new MissingReferenceException();
    }

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
        roundEndPanel.SetActive(false);

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
        if (hasRoundEnded)
            return;

        roundTimer = PhotonNetwork.Time - serverStartTime;

        if (PhotonNetwork.IsMasterClient)
        {
            if (gameMode.TryGetEarlyWinner(out int earlyWinner))
            {
                RaiseRoundEndedEvent(earlyWinner);
                return;
            }
            if (roundTimer >= roundTime)
            {
                RaiseRoundEndedEvent(gameMode.DetermineWinner());
                return;
            }
        }

        var remainingTime = Math.Max(0d, roundTime - roundTimer);
        TimeSpan t = TimeSpan.FromSeconds(remainingTime);
        timerText.text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Called by the owner of a killed player, reports the kill to the master client for scoring.
    /// </summary>
    public static void ReportPlayerKilled(int victimTeamID, int killerTeamID)
    {
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(PhotonEventCodes.PlayerKilled, new int[] { victimTeamID, killerTeamID }, raiseEventOptions, SendOptions.SendReliable);
    }
    #endregion

    #region Private Methods
    private void RaiseRoundEndedEvent(int winnerTeamID)
    {
        // Set immediately so the master doesn't raise the event again while waiting for it to arrive
        hasRoundEnded = true;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(PhotonEventCodes.RoundEnded, winnerTeamID, raiseEventOptions, SendOptions.SendReliable);
    }

    private void OnRoundEnded(int winnerTeamID)
    {
        hasRoundEnded = true;
        timerText.text = "00:00";
        GameManager.Instance.EndRound();

        if (winnerTeamID == GameMode.NoWinner)
            roundEndText.text = "Draw - the battle is undecided";
        else if (winnerTeamID == GameManager.Instance.TeamID)
            roundEndText.text = string.Format("{0} wins - Victory!", GetTeamName(winnerTeamID));
        else
            roundEndText.text = string.Format("{0} wins - Defeat!", GetTeamName(winnerTeamID));
        roundEndPanel.SetActive(true);

        StartCoroutine(LeaveRoomAfterDelay());
    }

    private string GetTeamName(int teamID)
    {
        if (teamID >= 0 && teamID < teamNames.Length)
            return teamNames[teamID];
        return "Team " + (teamID + 1);
    }

    private IEnumerator LeaveRoomAfterDelay()
    {
        yield return new WaitForSeconds(leaveRoomDelay);
        GameManager.Instance.LeaveRoom();
    }
    #endregion
}
