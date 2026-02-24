using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RLBot.Flat;

namespace RLBot.Manager;

/// <summary>
/// A simple bot manager than runs each bot on a different thread.
/// Ideal for simple hivemind bots that occasionally coordinate and share computation, but run mostly in parallel.
/// Be mindful with shared resources and static variables when using this manager.
/// The manager handles the bots' life-cycle including initialization, packet reading loop, and retirement on disconnect.
/// </summary>
/// <param name="rlbot">An rlbot connection interface</param>
/// <param name="defaultAgentId">A unique id for this type of bot. Should match the agent id in your bot.toml file and typically has the form "devname/botname/version".</param>
/// <param name="botFactory">A bot factory for creating instances of the bot once all required information has arrived.</param>
/// <seealso cref="SingleThreadBotManager"/>
/// <seealso cref="HivemindManager"/>
public class MultiThreadBotManager(
    RLBotInterface rlbot,
    string? defaultAgentId,
    BotFactory botFactory
) : AgentBaseManager(rlbot, defaultAgentId)
{
    private class BoxedBool(bool val)
    {
        public bool Value { get; set; } = val;
    }

    private record struct GameTickData(GamePacketT Packet, BallPredictionT? BallPred);

    private record BotInfo(
        IBot Bot,
        string Name,
        int Index,
        Thread Thread,
        BoxedBool Running,
        BlockingCollection<GameTickData> Queue,
        ConcurrentQueue<MatchCommT> MatchComms
    );

    private readonly List<BotInfo> _botInfos = new();

    protected override void Initialize()
    {
        Debug.Assert(_botInfos.Count == 0);

        // Initialize bots and start threads
        var playerConfs = MatchConfig.PlayerConfigurations;
        var team = TeamInfo.Team;
        foreach (var agent in TeamInfo.Controllables)
        {
            var index = (int)agent.Index;
            var name = playerConfs[index].Variety.AsCustomBot().Name;
            var bot = botFactory(
                new BotInitParams(Rlbot, index, team, name, AgentId, MatchConfig, FieldInfo)
            );
            var queue = new BlockingCollection<GameTickData>(
                new ConcurrentQueue<GameTickData>()
            );
            var matchComms = new ConcurrentQueue<MatchCommT>();
            var info = new BotInfo(
                bot,
                name,
                index,
                new Thread(BotLoop),
                new BoxedBool(true),
                queue,
                matchComms
            );
            _botInfos.Add(info);
            info.Thread.Start(info);
        }

        // Initial loadouts
        foreach (var info in _botInfos)
        {
            var loadout = info.Bot.GetInitialLoadout();
            if (loadout != null)
            {
                Rlbot.SendSetLoadout(
                    new SetLoadoutT { Index = (uint)info.Index, Loadout = loadout }
                );
            }
        }
    }

    private void BotLoop(object? botInfo)
    {
        var info = (BotInfo)botInfo!;
        try
        {
            while (info.Running.Value)
            {
                // Match comms
                while (info.MatchComms.TryDequeue(out var msg))
                {
                    try
                    {
                        info.Bot.OnMatchCommReceived(msg);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            "{} encountered an error while processing match comms: {}",
                            info.Name,
                            e
                        );
                        return;
                    }
                }

                // Game packet (temp blocking)
                if (!info.Queue.TryTake(out var data, 200))
                {
                    continue;
                }

                ControllerStateT? ctrl = null;
                try
                {
                    ctrl = info.Bot.GetOutput(data.Packet, data.BallPred);
                }
                catch (Exception e)
                {
                    Logger.LogError(
                        "{} encountered an error while processing game packet: {}",
                        info.Name,
                        e
                    );
                    return;
                }

                if (ctrl != null)
                {
                    Rlbot.SendPlayerInput(
                        new PlayerInputT
                        {
                            PlayerIndex = (uint)info.Index,
                            ControllerState = ctrl,
                        }
                    );
                }
            }
        }
        finally
        {
            info.Bot.OnRetire();
        }
    }

    protected override void ProcessPacket()
    {
        foreach (var info in _botInfos)
        {
            // Take previous packet if bot-loop did not already. Ensures that there is only one item in queue.
            info.Queue.TryTake(out _);
            info.Queue.Add(new GameTickData(_latestPacket!, _latestPrediction));
        }
    }

    protected override void HandleMatchComm(MatchCommT msg)
    {
        foreach (var info in _botInfos)
        {
            info.MatchComms.Enqueue(msg);
        }
    }

    protected override void Retire()
    {
        foreach (var info in _botInfos)
        {
            info.Running.Value = false;
        }
        foreach (var info in _botInfos)
        {
            info.Thread.Join();
        }
    }
}
