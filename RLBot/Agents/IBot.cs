using RLBot.Flat;

namespace RLBot.Manager;

/// <summary>An interface for simple bots.</summary>
/// <seealso cref="SingleThreadBotManager"/>
/// <seealso cref="MultiThreadBotManager"/>
public interface IBot
{
    /// <summary>
    /// Defines the bot's initial loadout. Return null to use the default loadout specified in the bot.toml file.
    /// To change loadout mid-match, use <see cref="RLBotInterface.SendSetLoadout"/> (state-setting must be enabled).
    /// </summary>
    PlayerLoadoutT? GetInitialLoadout();

    /// <summary>
    /// Process the latest game packet and return this bot's next action.
    /// </summary>
    /// <param name="packet">The latest game packet.</param>
    /// <param name="ballPrediction">The latest ball prediction. May be null if ball prediction was not requested.</param>
    /// <returns>The bots next action.</returns>
    ControllerStateT? GetOutput(GamePacketT packet, BallPredictionT? ballPrediction);

    /// <summary>
    /// Process an incoming match-comm message.
    /// See the index and team field to determine the sender.
    /// </summary>
    /// <param name="msg">A match-comm message</param>
    void OnMatchCommReceived(MatchCommT msg);

    /// <summary>
    /// Invoked when the bot is shut down. Use this to dispose of resources.
    /// </summary>
    void OnRetire();
}

/// <summary>A delegate for methods that can create <see cref="IBot"/> instances.</summary>
/// <seealso cref="SingleThreadBotManager"/>
/// <seealso cref="MultiThreadBotManager"/>
public delegate IBot BotFactory(BotInitParams ctx);

/// <summary>
/// A plain struct containing the parameters of a bot.
/// </summary>
/// <param name="rlbot">A reference to the RLBotInterface for communication with the server.</param>
/// <param name="index">The index of the bot.</param>
/// <param name="team">The team of the bot.</param>
/// <param name="name">The name of the bot as it appears in-game.</param>
/// <param name="agentId">The agent id of the bot.</param>
/// <param name="matchConfig">The match configuration defining the current match.</param>
/// <param name="fieldInfo">Static information about the map such as boost pad layout.</param>
public struct BotInitParams(
    RLBotInterface rlbot,
    int index,
    uint team,
    string name,
    string agentId,
    MatchConfigurationT matchConfig,
    FieldInfoT fieldInfo
)
{
    public readonly RLBotInterface Rlbot = rlbot;
    public readonly int Index = index;
    public readonly uint Team = team;
    public readonly string Name = name;
    public readonly string AgentId = agentId;
    public readonly MatchConfigurationT MatchConfig = matchConfig;
    public readonly FieldInfoT FieldInfo = fieldInfo;
}

/// <summary>
/// An abstract bot. Declares fields for the bot parameters but is otherwise empty.
/// </summary>
public abstract class AbstractBot(BotInitParams botParams) : IBot
{
    /// A reference to the RLBotInterface for communication with the server.
    public readonly RLBotInterface Rlbot = botParams.Rlbot;
        
    /// The index of the bot.
    public readonly int Index = botParams.Index;
    
    /// The team of the bot.
    public readonly uint Team = botParams.Team;
    
    /// The name of the bot as it appears in-game.
    public readonly string Name = botParams.Name;
    
    /// The agent id of the bot.
    public readonly string AgentId = botParams.AgentId;
    
    /// The match configuration defining the current match.
    public readonly MatchConfigurationT MatchConfig = botParams.MatchConfig;
    
    /// Static information about the map such as boost pad layout.
    public readonly FieldInfoT FieldInfo = botParams.FieldInfo;

    public abstract PlayerLoadoutT? GetInitialLoadout();
    public abstract ControllerStateT? GetOutput(GamePacketT packet, BallPredictionT? ballPrediction);
    public abstract void OnMatchCommReceived(MatchCommT msg);
    public abstract void OnRetire();
}
