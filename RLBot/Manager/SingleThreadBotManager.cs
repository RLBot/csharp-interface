using Microsoft.Extensions.Logging;
using RLBot.Flat;

namespace RLBot.Manager;

public class SingleThreadBotManager(
    RLBotInterface rlbot,
    string defaultAgentId,
    Func<
        RLBotInterface,
        int,
        uint,
        string,
        string,
        MatchConfigurationT,
        FieldInfoT,
        IBot
    > botFactory
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

    protected override void HandleMatchComm(MatchCommT comm)
    {
        foreach (var botInfo in _botInfos)
        {
            try
            {
                botInfo.Bot.OnMatchCommReceived(comm);
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
