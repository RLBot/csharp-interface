using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBot.GameState;
using RLBot.Util;

namespace RLBot.Manager;

public abstract class Script
{
    public Logging Logger = new("Script", LogLevel.Information);

    public int Index { get; private set; }
    public string Name { get; private set; } = "UnknownScript";

    public MatchConfigurationT MatchConfig { get; private set; } = new();
    public FieldInfoT FieldInfo { get; private set; } = new();
    public BallPredictionT BallPrediction { get; private set; } = new();

    public readonly Renderer Renderer;

    private bool _initialized = false;
    private bool _hasMatchSettings = false;
    private bool _hasFieldInfo = false;

    private readonly RLBotInterface _rlbotInterface;
    private GamePacketT? _latestPacket;
    private BallPredictionT _latestPrediction = new();

    public Script(string? defaultAgentId = null)
    {
        string? agentId =
            Environment.GetEnvironmentVariable("RLBOT_AGENT_ID") ?? defaultAgentId;

        if (agentId is null)
        {
            Logger.LogCritical(
                "Environment variable RLBOT_AGENT_ID is not set and no default agent id is passed to "
                    + "the constructor of the script. If you are starting your script manually, please set it "
                    + "manually, e.g. `RLBOT_AGENT_ID=<agent_id> dotnet run`"
            );

            throw new Exception(
                "Environment variable RLBOT_AGENT_ID is not set and no default agent id is passed to the constructor of the bot."
            );
        }

        _rlbotInterface = new RLBotInterface(agentId, logger: Logger);
        _rlbotInterface.OnMatchConfigCallback += HandleMatchConfig;
        _rlbotInterface.OnFieldInfoCallback += HandleFieldInfo;
        _rlbotInterface.OnMatchCommunicationCallback += HandleMatchCommunication;
        _rlbotInterface.OnBallPredictionCallback += HandleBallPrediction;
        _rlbotInterface.OnGamePacketCallback += HandleGamePacket;

        Renderer = new Renderer(_rlbotInterface);
    }

    private void TryInitialize()
    {
        if (_initialized || !_hasMatchSettings || !_hasFieldInfo)
            return;

        try
        {
            Initialize();
        }
        catch (Exception e)
        {
            Logger.LogCritical(
                "Script {0} failed to initialize due the following error: {1}",
                Name,
                e
            );
            throw new Exception("Failed to initialize script.", e);
        }

        _initialized = true;
        _rlbotInterface.SendInitComplete();
    }

    public virtual void Initialize() { }

    private void HandleMatchConfig(MatchConfigurationT matchConfig)
    {
        MatchConfig = matchConfig;

        for (int i = 0; i < matchConfig.ScriptConfigurations.Count; i++)
        {
            var script = matchConfig.ScriptConfigurations[i];
            if (script.AgentId == _rlbotInterface.AgentId)
            {
                Index = i;
                Name = script.Name;
                _hasMatchSettings = true;
            }
        }

        if (!_hasMatchSettings)
        {
            Logger.LogWarning("Script with agent id '{}' did not find itself in the match settings", _rlbotInterface.AgentId);
        }
        
        TryInitialize();
    }

    private void HandleFieldInfo(FieldInfoT fieldInfo)
    {
        FieldInfo = fieldInfo;
        _hasFieldInfo = true;
        TryInitialize();
    }

    private void HandleMatchCommunication(MatchCommT matchComm) =>
        HandleMatchComm(
            (int)matchComm.Index,
            (int)matchComm.Team,
            matchComm.Content,
            matchComm.Display,
            matchComm.TeamOnly
        );

    public virtual void HandleMatchComm(
        int Index,
        int Team,
        List<byte> Content,
        string? Display,
        bool teamOnly
    ) { }

    public void SendMatchComm(
        int Index,
        int Team,
        List<byte> Content,
        string? Display = null,
        bool teamOnly = false
    )
    {
        _rlbotInterface.SendMatchComm(
            new MatchCommT
            {
                Index = (uint)Index,
                Team = (uint)Team,
                Content = Content,
                Display = Display,
                TeamOnly = teamOnly,
            }
        );
    }

    private void HandleBallPrediction(BallPredictionT ballPrediction) =>
        _latestPrediction = ballPrediction;

    private void HandleGamePacket(GamePacketT gamePacket) => _latestPacket = gamePacket;

    private void ProcessPacket(GamePacketT packet)
    {
        BallPrediction = _latestPrediction;

        try
        {
            HandlePacket(packet);
        }
        catch (Exception e)
        {
            Logger.LogError(
                "Script {0} encountered an error while processing game packet: {1}",
                Name,
                e
            );
        }
    }

    public void Run(bool wantsMatchCommunications = true, bool wantsBallPredictions = true)
    {
        int rlbotServerPort = int.Parse(
            Environment.GetEnvironmentVariable("RLBOT_SERVER_PORT") ?? RLBotInterface.DEFAULT_RLBOT_SERVER_PORT.ToString()
        );

        try
        {
            _rlbotInterface.Connect(
                wantsMatchCommunications,
                wantsBallPredictions,
                rlbotServerPort: rlbotServerPort
            );

            while (true)
            {
                var res = _rlbotInterface.HandleIncomingMessages(
                    blocking: _latestPacket is null
                );

                switch (res)
                {
                    case RLBotInterface.MsgHandlingResult.Terminated:
                        return;
                    case RLBotInterface.MsgHandlingResult.MoreMsgsQueued:
                        continue;
                    case RLBotInterface.MsgHandlingResult.NoIncomingMsgs:
                        if (_latestPacket is not null)
                        {
                            ProcessPacket(_latestPacket);
                            _latestPacket = null;
                        }
                        continue;
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogCritical("An error occured while running the bot:\n{0}", e);
        }
        finally
        {
            Retire();
        }
    }

    public void SetLoadout(SetLoadoutT setLoadout) =>
        _rlbotInterface.SendSetLoadout(setLoadout);

    /// <summary>
    /// Modify the current game state using a builder pattern.
    /// </summary>
    public DesiredGameStateBuilder GameStateBuilder()
    {
        return new DesiredGameStateBuilder(_rlbotInterface);
    }
    
    public void SetGameState(
        Dictionary<int, DesiredBallStateT>? balls = null,
        Dictionary<int, DesiredCarStateT>? cars = null,
        DesiredMatchInfoT? matchInfo = null,
        List<ConsoleCommandT>? commands = null
    )
    {
        var gameState = GameStateExt.FillDesiredGameState(balls, cars, matchInfo, commands);
        _rlbotInterface.SendGameState(gameState);
    }

    public virtual void Retire() { }

    public abstract void HandlePacket(GamePacketT packet);
}
