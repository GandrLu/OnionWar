using Photon.Pun;

/// <summary>
/// Base class for game modes (e.g. team deathmatch, conquest). Decides how kills are scored and which team wins.
/// Scoring and win evaluation only run on the master client, results are shared via room properties and events.
/// </summary>
public abstract class GameMode : MonoBehaviourPunCallbacks
{
    public const int TeamCount = 2;
    public const int NoWinner = -1;

    #region Public Methods
    /// <summary>
    /// Called on the master client whenever a player got killed.
    /// </summary>
    public abstract void OnPlayerKilled(int victimTeamID, int killerTeamID);

    /// <summary>
    /// Checked by the master client every frame, allows a mode to end the round before the time runs out.
    /// </summary>
    public virtual bool TryGetEarlyWinner(out int winnerTeamID)
    {
        winnerTeamID = NoWinner;
        return false;
    }

    /// <summary>
    /// Called on the master client when the round time is over. Returns the winning team ID or NoWinner on a draw.
    /// </summary>
    public abstract int DetermineWinner();
    #endregion
}
