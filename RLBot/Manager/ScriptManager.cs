using Microsoft.Extensions.Logging;
using RLBot.Flat;

namespace RLBot.Manager;

public class ScriptManager(
    RLBotInterface rlbot,
    string defaultAgentId,
    Func<RLBotInterface, int, string, MatchConfigurationT, FieldInfoT, IScript> scriptFactory
) : AgentBaseManager(rlbot, defaultAgentId)
{
    private IScript? _script;
    private int _index;
    private string _name;

    protected override void Initialize()
    {
        var agent = TeamInfo.Controllables[0];
        _index = (int)agent.Index;
        _name = MatchConfig.ScriptConfigurations[_index].Name;
        _script = scriptFactory(Rlbot, _index, AgentId, MatchConfig, FieldInfo);
    }

    protected override void ProcessPacket()
    {
        if (_script == null)
            return;
        try
        {
            _script.ProcessPacket(_latestPacket!, _latestPrediction);
        }
        catch (Exception e)
        {
            Logger.LogError(
                "{} encountered an error while processing game packet: {}",
                _name,
                e
            );
        }
    }

    protected override void HandleMatchComm(MatchCommT matchComm)
    {
        if (_script == null)
            return;
        try
        {
            _script.OnMatchCommReceived(matchComm);
        }
        catch (Exception e)
        {
            Logger.LogError(
                "{} encountered an error while processing match comms: {}",
                _name,
                e
            );
        }
    }

    protected override void Retire()
    {
        if (_script == null)
            return;
        try
        {
            _script.OnRetire();
        }
        catch (Exception e)
        {
            Logger.LogError("{} encountered an error while retiring: {}", _name, e);
        }
    }
}
