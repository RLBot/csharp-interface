using Microsoft.Extensions.Logging;
using RLBot.Flat;

namespace RLBot.Manager;

/// <summary>A delegate for methods that can create <see cref="IScript"/> instances.</summary>
/// <seealso cref="ScriptManager"/>
public delegate IScript ScriptFactory(RLBotInterface rlbot, int index, string agentId, MatchConfigurationT matchConfig, FieldInfoT fieldInfo);

/// <summary>
/// A simple manager for scripts. Scripts observe the match and potentially uses debug rendering, state-setting, or match comms.
/// The manager handles the script's life-cycle including initialization, packet reading loop, and retirement on disconnect.
/// </summary>
/// <param name="rlbot">An rlbot connection interface</param>
/// <param name="defaultAgentId">A unique id for this type of script. Should match the agent id in your script.toml file and typically has the form "devname/scriptname/version".</param>
/// <param name="scriptFactory">A factory for creating the script instance once all required information has arrived.</param>
public class ScriptManager(
    RLBotInterface rlbot,
    string defaultAgentId,
    ScriptFactory scriptFactory
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

    protected override void HandleMatchComm(MatchCommT msg)
    {
        if (_script == null)
            return;
        try
        {
            _script.OnMatchCommReceived(msg);
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
