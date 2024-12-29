using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBot.Util;

namespace RLBot.Manager;

public abstract class Script
{
    public Logging Logger = new("rlbot", LogLevel.Information);

    public int Index { get; private set; }
    public string Name { get; private set; }
    
    public MatchSettingsT MatchSettings { get; private set; } = new();
    public FieldInfoT FieldInfo { get; private set; } = new();
    public BallPredictionT BallPrediction { get; private set; } = new();
    
    private bool _initialized = false;
    private bool _hasMatchSettings = false;
    private bool _hasFieldInfo = false;
    
    private readonly Interface _gameInterface;
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
        
        Logger = new Logging("Script", LogLevel.Information);
        _gameInterface = new Interface(agentId, logger: Logger);
        _gameInterface.OnMatchSettingsCallback += HandleMatchSettings;
        _gameInterface.OnFieldInfoCallback += HandleFieldInfo;
        _gameInterface.OnMatchCommunicationCallback += HandleMatchCommunication;
        _gameInterface.OnBallPredictionCallback += HandleBallPrediction;
        _gameInterface.OnGamePacketCallback += HandleGamePacket;
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
        _gameInterface.SendInitComplete();
    }
    
    public virtual void Initialize() { }
    
    private void HandleMatchSettings(MatchSettingsT matchSettings)
    {
        MatchSettings = matchSettings;
        _hasMatchSettings = true;
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
        _gameInterface.SendMatchComm(
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
            Environment.GetEnvironmentVariable("RLBOT_SERVER_PORT") ?? "23234"
        );

        try
        {
            _gameInterface.Connect(
                wantsMatchCommunications,
                wantsBallPredictions,
                rlbotServerPort: rlbotServerPort
            );

            bool running = true;
            while (running)
            {
                running = _gameInterface.HandleIncomingMessages(
                    blocking: _latestPacket is null
                );

                if (_latestPacket is not null && running)
                {
                    ProcessPacket(_latestPacket);
                    _latestPacket = null;
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
        _gameInterface.SendSetLoadout(setLoadout);

    public void SetGameState(
        Dictionary<int, DesiredBallStateT>? balls = null,
        Dictionary<int, DesiredCarStateT>? cars = null,
        DesiredGameInfoStateT? gameInfo = null,
        List<ConsoleCommandT>? commands = null
    )
    {
        var gameState = GameStateExt.FillDesiredGameState(balls, cars, gameInfo, commands);
        _gameInterface.SendGameState(gameState);
    }

    public virtual void Retire() { }

    public abstract void HandlePacket(GamePacketT packet);
}