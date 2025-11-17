using RLBot.Flat;

namespace RLBot.Manager;

/// <summary>An interface for simple bots.</summary>
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
