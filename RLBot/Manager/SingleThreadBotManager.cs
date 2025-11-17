using Microsoft.Extensions.Logging;
using RLBot.Flat;

namespace RLBot.Manager;

/// <summary>A delegate for methods that can create <see cref="IBot"/> instances.</summary>
public delegate IBot BotFactory(RLBotInterface rlbot, int index, uint team, string name, string agentId, MatchConfigurationT matchConfig, FieldInfoT fieldInfo);

/// <summary>
/// A simple bot manager than runs everything on a single thread.
/// Ideal for standard, non-hivemind bots. Hiveminds are support too, but consider other managers.
/// The manager handles the bot(s)'s life-cycle including initialization, packet reading loop, and retirement on disconnect.
/// </summary>
/// <param name="rlbot">An rlbot connection interface</param>
/// <param name="defaultAgentId">A unique id for this type of bot. Should match the agent id in your bot.toml file and typically has the form "devname/botname/version".</param>
/// <param name="botFactory">A bot factory for creating instances of the bot once all required information has arrived.</param>
public class SingleThreadBotManager(
    RLBotInterface rlbot,
    string defaultAgentId,
    BotFactory botFactory
) : AgentBaseManager(rlbot, defaultAgentId)
{
    private record BotInfo(IBot Bot, string Name, int Index);

    private readonly List<BotInfo> _botInfos = new();

    protected override void Initialize()
    {
        // Create bots
        var playerConfigs = MatchConfig.PlayerConfigurations;
        var team = TeamInfo.Team;
        _botInfos.Clear();
        foreach (var agent in TeamInfo.Controllables)
        {
            var index = (int)agent.Index;
            var name = playerConfigs[index].Variety.AsCustomBot().Name;
            var bot = botFactory(Rlbot, index, team, name, AgentId, MatchConfig, FieldInfo);
            var process = new BotInfo(bot, name, index);
            _botInfos.Add(process);
        }

        // Set loadouts
        foreach (var botInfo in _botInfos)
        {
            var loadout = botInfo.Bot.GetInitialLoadout();
            if (loadout != null)
            {
                Rlbot.SendSetLoadout(
                    new SetLoadoutT { Index = (uint)botInfo.Index, Loadout = loadout }
                );
            }
        }
    }

    protected override void ProcessPacket()
    {
        foreach (var botInfo in _botInfos)
        {
            try
            {
                var ctrl = botInfo.Bot.GetOutput(_latestPacket!, _latestPrediction);
                if (ctrl != null)
                {
                    Rlbot.SendPlayerInput(
                        new PlayerInputT
                        {
                            PlayerIndex = (uint)botInfo.Index,
                            ControllerState = ctrl,
                        }
                    );
                }
            }
            catch (Exception e)
            {
                Logger.LogError(
                    "{} encountered an error while processing game packet: {}",
                    botInfo.Name,
                    e
                );
            }
        }
    }

    protected override void HandleMatchComm(MatchCommT msg)
    {
        foreach (var botInfo in _botInfos)
        {
            try
            {
                botInfo.Bot.OnMatchCommReceived(msg);
            }
            catch (Exception e)
            {
                Logger.LogError(
                    "{} encountered an error while processing match comms: {}",
                    botInfo.Name,
                    e
                );
            }
        }
    }

    protected override void Retire()
    {
        foreach (var botInfo in _botInfos)
        {
            try
            {
                botInfo.Bot.OnRetire();
            }
            catch (Exception e)
            {
                Logger.LogError("{} encountered an error while retiring: {}", botInfo.Name, e);
            }
        }
    }
}
