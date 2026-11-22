using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;

/// <summary>
/// Every kill scores a point for the killer's team. The team with the most kills when the time runs out wins.
/// </summary>
public sealed class TeamDeathmatchMode : GameMode
{
    #region Serialized Fields
    [Tooltip("Score text per team, index is the team ID")]
    [SerializeField] TMP_Text[] teamScoreTexts;
    #endregion

    #region Private Fields
    private const string TeamKillsKey = "TeamKills";
    private int[] teamKills = new int[TeamCount];
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        if (teamScoreTexts == null || teamScoreTexts.Length != TeamCount)
            throw new MissingReferenceException();
        foreach (var text in teamScoreTexts)
            if (text == null)
                throw new MissingReferenceException();
    }

    private void Start()
    {
        LoadTeamKillsFromRoom();
        UpdateScoreTexts();
    }
    #endregion

    #region Photon Callbacks
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // The master client holds the authoritative count, it would be overwritten by its own delayed echoes otherwise
        if (PhotonNetwork.IsMasterClient || !propertiesThatChanged.ContainsKey(TeamKillsKey))
            return;

        teamKills = (int[])propertiesThatChanged[TeamKillsKey];
        UpdateScoreTexts();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
            LoadTeamKillsFromRoom();
    }
    #endregion

    #region Public Methods
    public override void OnPlayerKilled(int victimTeamID, int killerTeamID)
    {
        // Team kills don't score
        if (killerTeamID == victimTeamID || killerTeamID < 0 || killerTeamID >= TeamCount)
            return;

        teamKills[killerTeamID]++;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { TeamKillsKey, teamKills } });
        UpdateScoreTexts();
    }

    public override int DetermineWinner()
    {
        int winner = NoWinner;
        int mostKills = -1;
        for (int i = 0; i < TeamCount; i++)
        {
            if (teamKills[i] > mostKills)
            {
                mostKills = teamKills[i];
                winner = i;
            }
            else if (teamKills[i] == mostKills)
            {
                winner = NoWinner;
            }
        }
        return winner;
    }
    #endregion

    #region Private Methods
    private void LoadTeamKillsFromRoom()
    {
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TeamKillsKey, out var kills))
            teamKills = (int[])kills;
    }

    private void UpdateScoreTexts()
    {
        for (int i = 0; i < TeamCount; i++)
            teamScoreTexts[i].text = teamKills[i].ToString();
    }
    #endregion
}
