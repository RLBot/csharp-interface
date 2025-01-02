using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBot.Util;

namespace RLBot.Manager;

public abstract class Bot
{
    public Logging Logger = new("rlbot", LogLevel.Information);

    public int Team { get; private set; } = -1;
    public int Index { get; private set; } = -1;
    public string Name { get; private set; } = "";
    public int SpawnId { get; private set; } = -1;

    public MatchSettingsT MatchSettings { get; private set; } = new();
    public FieldInfoT FieldInfo { get; private set; } = new();
    public BallPredictionT BallPrediction { get; private set; } = new();

    public readonly Renderer Renderer;

    private bool _initializedBot = false;
    private bool _hasMatchSettings = false;
    private bool _hasFieldInfo = false;
    private bool _hasPlayerMapping = false;

    private readonly Interface _gameInterface;
    private GamePacketT? _latestPacket;
    private BallPredictionT _latestPrediction = new();

    public Bot(string? defaultAgentId = null)
    {
        string? agentId =
            Environment.GetEnvironmentVariable("RLBOT_AGENT_ID") ?? defaultAgentId;

        if (agentId is null)
        {
            Logger.LogCritical(
                "Environment variable RLBOT_AGENT_ID is not set and no default agent id is passed to "
                    + "the constructor of the bot. If you are starting your bot manually, please set it "
                    + "manually, e.g. `RLBOT_AGENT_ID=<agent_id> dotnet run`"
            );

            throw new Exception(
                "Environment variable RLBOT_AGENT_ID is not set and no default agent id is passed to the constructor of the bot."
            );
        }

        Logger = new Logging("Bot", LogLevel.Information);
        _gameInterface = new Interface(agentId, logger: Logger);
        _gameInterface.OnMatchSettingsCallback += HandleMatchSettings;
        _gameInterface.OnFieldInfoCallback += HandleFieldInfo;
        _gameInterface.OnMatchCommunicationCallback += HandleMatchCommunication;
        _gameInterface.OnBallPredictionCallback += HandleBallPrediction;
        _gameInterface.OnControllableTeamInfoCallback += HandleControllableTeamInfo;
        _gameInterface.OnGamePacketCallback += HandleGamePacket;

        Renderer = new Renderer(_gameInterface);
    }

    private void TryInitialize()
    {
        if (_initializedBot || !_hasMatchSettings || !_hasFieldInfo || !_hasPlayerMapping)
            return;

        foreach (PlayerConfigurationT player in MatchSettings.PlayerConfigurations)
        {
            if (player.SpawnId == SpawnId)
            {
                Name = player.Name;
                Logger = new Logging(Name, LogLevel.Information);
                break;
            }
        }

        try
        {
            Initialize();
        }
        catch (Exception e)
        {
            Logger.LogCritical(
                "Bot {0} failed to initialize due the following error: {1}",
                Name,
                e
            );
            throw new Exception("Failed to initialize bot.", e);
        }

        _initializedBot = true;
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

    private void HandleControllableTeamInfo(ControllableTeamInfoT controllableTeamInfo)
    {
        Team = (int)controllableTeamInfo.Team;
        var controllable = controllableTeamInfo.Controllables[0];
        Index = (int)controllable.Index;
        SpawnId = controllable.SpawnId;
        _hasPlayerMapping = true;

        TryInitialize();
    }

    private void HandleGamePacket(GamePacketT gamePacket) => _latestPacket = gamePacket;

    private void ProcessPacket(GamePacketT packet)
    {
        if (packet.Players.Count <= Index)
        {
            return;
        }

        BallPrediction = _latestPrediction;
        ControllerStateT controller;

        try
        {
            controller = GetOutput(packet);
        }
        catch (Exception e)
        {
            Logger.LogError(
                "Bot {0} encountered an error while processing game packet: {1}",
                Name,
                e
            );
            return;
        }

        var playerInput = new PlayerInputT
        {
            PlayerIndex = (uint)Index,
            ControllerState = controller,
        };
        _gameInterface.SendPlayerInput(playerInput);
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
            return;
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

    public abstract ControllerStateT GetOutput(GamePacketT packet);
}
