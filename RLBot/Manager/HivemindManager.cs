using Microsoft.Extensions.Logging;
using RLBot.Flat;

namespace RLBot.Manager;

/// <summary>A delegate for methods that can create <see cref="IHivemind"/> instances.</summary>
/// <seealso cref="HivemindManager"/>
public delegate IHivemind HivemindFactory(RLBotInterface rlbot, List<int> indices, uint team, Dictionary<int, string> names, string agentId, MatchConfigurationT matchConfig, FieldInfoT fieldInfo);

/// <summary>
/// A manager for hivemind bots.
/// A hivemind is a single process responsible for multiple cars.
/// This manager allows the user to customize the orchestration of the entire swarm.
/// The manager handles the hivemind's life-cycle including initialization, packet reading loop, and retirement on disconnect.
/// </summary>
/// <param name="rlbot">An rlbot connection interface</param>
/// <param name="defaultAgentId">A unique id for this type of bot. Should match the agent id in your bot.toml file and typically has the form "devname/botname/version".</param>
/// <param name="hivemindFactory">A factory for creating the hivemind instance once all required information has arrived.</param>
public class HivemindManager(
    RLBotInterface rlbot,
    string? defaultAgentId,
    HivemindFactory hivemindFactory
)
    : AgentBaseManager(rlbot, defaultAgentId)
{
    private IHivemind? _hivemind;
    private List<int> _indices;
    private uint _team;
    
    protected override void Initialize()
    {
        var playerConfs = MatchConfig.PlayerConfigurations;
        _team = TeamInfo.Team;
        _indices = TeamInfo.Controllables.Select(a => (int)a.Index).ToList();
        var names = _indices.ToDictionary(i => i, i => playerConfs[i].Variety.AsCustomBot().Name);
        
        _hivemind = hivemindFactory(Rlbot, _indices, _team, names, AgentId, MatchConfig, FieldInfo);

        var loadouts = _hivemind.GetInitialLoadouts();
        if (loadouts != null)
        {
            foreach (var loadout in loadouts)
            {
                Rlbot.SendSetLoadout(new SetLoadoutT
                {
                    Index = (uint)loadout.Key,
                    Loadout = loadout.Value
                });
            }
        }
    }

    protected override void ProcessPacket()
    {
        if (_hivemind == null) return;

        IDictionary<int, ControllerStateT>? controllers = null;
        try
        {
            controllers = _hivemind.GetOutputs(_latestPacket, _latestPrediction);
        }
        catch (Exception e)
        {
            Logger.LogError("Hivemind '{}' (team {}) encountered an error while processing game packet: {}", AgentId, _team, e);
        }

        if (controllers != null)
        {
            foreach (var ctrl in controllers)
            {
                Rlbot.SendPlayerInput(new PlayerInputT
                {
                    PlayerIndex = (uint)ctrl.Key,
                    ControllerState = ctrl.Value
                });
            }
        }
    }

    protected override void HandleMatchComm(MatchCommT msg)
    {
        if (_hivemind == null) return;

        try
        {
            _hivemind.OnMatchCommReceived(msg);
        }
        catch (Exception e)
        {
            Logger.LogError("Hivemind '{}' (team {}) encountered an error while processing match comms: {}", AgentId, _team, e);
        }
    }

    protected override void Retire()
    {
        if (_hivemind == null) return;
        
        try
        {
            _hivemind.OnRetire();
        }
        catch (Exception e)
        {
            Logger.LogError("Hivemind '{}' (team {}) encountered an error while retiring: {}", AgentId, _team, e);
        }
    }
}
