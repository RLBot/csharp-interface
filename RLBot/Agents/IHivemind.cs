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
    IDictionary<int, ControllerStateT>? GetOutputs(
        GamePacketT packet,
        BallPredictionT? ballPred
    );

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
public delegate IHivemind HivemindFactory(
    RLBotInterface rlbot,
    List<int> indices,
    uint team,
    Dictionary<int, string> names,
    string agentId,
    MatchConfigurationT matchConfig,
    FieldInfoT fieldInfo
);

/// <summary>
/// A plain struct containing the parameters of a hivemind.
/// </summary>
/// <param name="rlbot">A reference to the RLBotInterface for communication with the server.</param>
/// <param name="indices">The bot indices that the hivemind controls.</param>
/// <param name="team">The team of the hivemind.</param>
/// <param name="names">A mapping from bot index to bot name as it appears in-game.</param>
/// <param name="agentId">The agent id of the bot.</param>
/// <param name="matchConfig">The match configuration defining the current match.</param>
/// <param name="fieldInfo">Static information about the map such as boost pad layout.</param>
public struct HivemindInitParams(
    RLBotInterface rlbot,
    List<int> indices,
    uint team,
    Dictionary<int, string> names,
    string agentId,
    MatchConfigurationT matchConfig,
    FieldInfoT fieldInfo
)
{
    public readonly RLBotInterface Rlbot = rlbot;
    public readonly List<int> Indices = indices;
    public readonly uint Team = team;
    public readonly Dictionary<int, string> Names = names;
    public readonly string AgentId = agentId;
    public readonly MatchConfigurationT MatchConfig = matchConfig;
    public readonly FieldInfoT FieldInfo = fieldInfo;
}

/// <summary>
/// An abstract hivemind. Declares fields for the hivemind parameters but is otherwise empty.
/// </summary>
public abstract class AbstractHivemind(HivemindInitParams hivemindParams) : IHivemind
{
    /// A reference to the RLBotInterface for communication with the server.
    public readonly RLBotInterface Rlbot = hivemindParams.Rlbot;

    /// The bot indices that the hivemind controls.
    public readonly List<int> Indices = hivemindParams.Indices;

    /// The team of the hivemind.
    public readonly uint Team = hivemindParams.Team;

    /// A mapping from bot index to bot name as it appears in-game.
    public readonly Dictionary<int, string> Names = hivemindParams.Names;

    /// The agent id of the bot.
    public readonly string AgentId = hivemindParams.AgentId;

    /// The match configuration defining the current match.
    public readonly MatchConfigurationT MatchConfig = hivemindParams.MatchConfig;

    /// Static information about the map such as boost pad layout.
    public readonly FieldInfoT FieldInfo = hivemindParams.FieldInfo;

    public abstract IDictionary<int, PlayerLoadoutT>? GetInitialLoadouts();
    public abstract IDictionary<int, ControllerStateT>? GetOutputs(
        GamePacketT packet,
        BallPredictionT? ballPred
    );
    public abstract void OnMatchCommReceived(MatchCommT msg);
    public abstract void OnRetire();
}
