using RLBot.Flat;

namespace RLBot.Manager;

/// <summary>An interface for scripts.</summary>
/// <seealso cref="ScriptManager"/>
public interface IScript
{
    /// <summary>
    /// Process the latest game packet.
    /// </summary>
    /// <param name="packet">The latest game packet.</param>
    /// <param name="ballPrediction">The latest ball prediction. May be null if ball prediction was not requested.</param>
    void ProcessPacket(GamePacketT gamePacket, BallPredictionT? ballPrediction);

    /// <summary>
    /// Process an incoming match-comm message.
    /// See the index and team field to determine the sender.
    /// </summary>
    /// <param name="msg">A match-comm message</param>
    void OnMatchCommReceived(MatchCommT msg);

    /// <summary>
    /// Invoked when the script is shut down. Use this to dispose of resources.
    /// </summary>
    void OnRetire();
}

/// <summary>A delegate for methods that can create <see cref="IScript"/> instances.</summary>
/// <seealso cref="ScriptManager"/>
public delegate IScript ScriptFactory(ScriptInitParams scriptParams);

/// <summary>
/// A plain struct containing the parameters of a script.
/// </summary>
/// <param name="rlbot">A reference to the RLBotInterface for communication with the server.</param>
/// <param name="index">The index of the script.</param>
/// <param name="agentId">The agent id of the script.</param>
/// <param name="matchConfig">The match configuration defining the current match.</param>
/// <param name="fieldInfo">Static information about the map such as boost pad layout.</param>
public struct ScriptInitParams(
    RLBotInterface rlbot,
    int index,
    string agentId,
    MatchConfigurationT matchConfig,
    FieldInfoT fieldInfo
)
{
    public readonly RLBotInterface Rlbot = rlbot;
    public readonly int Index = index;
    public readonly string AgentId = agentId;
    public readonly MatchConfigurationT MatchConfig = matchConfig;
    public readonly FieldInfoT FieldInfo = fieldInfo;
}

/// <summary>
/// An abstract script. Declares fields for the script parameters but is otherwise empty.
/// </summary>
public abstract class AbstractScript(ScriptInitParams scriptParams) : IScript
{
    /// A reference to the RLBotInterface for communication with the server.
    public readonly RLBotInterface Rlbot = scriptParams.Rlbot;

    /// The index of the script.
    public readonly int Index = scriptParams.Index;

    /// The agent id of the script.
    public readonly string AgentId = scriptParams.AgentId;

    /// The match configuration defining the current match.
    public readonly MatchConfigurationT MatchConfig = scriptParams.MatchConfig;

    /// Static information about the map such as boost pad layout.
    public readonly FieldInfoT FieldInfo = scriptParams.FieldInfo;

    public abstract void ProcessPacket(
        GamePacketT gamePacket,
        BallPredictionT? ballPrediction
    );
    public abstract void OnMatchCommReceived(MatchCommT msg);
    public abstract void OnRetire();
}
