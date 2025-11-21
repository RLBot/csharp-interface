using RLBot.Flat;

namespace RLBot.Manager;

/// <summary>
/// An interface for a hivemind, a single process responsible for multiple cars.
/// </summary>
/// <seealso cref="HivemindManager"/>
public interface IHivemind
{
    /// <summary>
    /// Programmatically define the hiveminds initial loadout by mapping bot indexes to the desired loadout.
    /// Return null to use the default loadout specified in the bot.toml file.
    /// To change loadout mid-match, use <see cref="RLBotInterface.SendSetLoadout"/> (state-setting must be enabled).
    /// </summary>
    IDictionary<int, PlayerLoadoutT>? GetInitialLoadouts();
    
    /// <summary>
    /// Process the latest game packet and return the next action for each member of the hivemind.
    /// </summary>
    /// <param name="packet">The latest game packet.</param>
    /// <param name="ballPred">The latest ball prediction. May be null if ball prediction was not requested.</param>
    /// <returns>The bots' next actions.</returns>
    IDictionary<int, ControllerStateT>? GetOutputs(GamePacketT packet, BallPredictionT? ballPred);
    
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

/// <summary>A delegate for methods that can create <see cref="IHivemind"/> instances.</summary>
/// <seealso cref="HivemindManager"/>
public delegate IHivemind HivemindFactory(RLBotInterface rlbot, List<int> indices, uint team, Dictionary<int, string> names, string agentId, MatchConfigurationT matchConfig, FieldInfoT fieldInfo);
