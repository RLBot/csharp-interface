using RLBot.Flat;

namespace RLBot.Manager;

/// <summary>
/// An interface for a hivemind, a single process responsible for multiple cars. 
/// </summary>
/// <seealso cref="HivemindManager"/>
public interface IHivemind
{
    IDictionary<int, PlayerLoadoutT>? GetInitialLoadouts();
    
    IDictionary<int, ControllerStateT>? GetOutputs(GamePacketT packet, BallPredictionT ballPred);
    
    /// <summary>
    /// Process an incoming match-comm message.
    /// See the index and team field to determine the sender.
    /// </summary>
    /// <param name="msg">A match-comm message</param>
    void OnMatchCommReceived(MatchCommT msg);

    /// <summary>
    /// Invoked when the hivemind is shut down. Use this to dispose of resources.
    /// </summary>
    void OnRetire();
}
