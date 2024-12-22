using Microsoft.Extensions.Logging;
using rlbot;
using rlbot.flat;
using rlbot.Util;

public abstract class Bot
{
    public Logging Logger = new Logging("rlbot", LogLevel.Information);

    public int Team = -1;
    public int Index = -1;
    public string Name = "";
    public int SpawnId = -1;

    public MatchSettingsT MatchSettings = new();
    public FieldInfoT FieldInfo = new();
    public BallPredictionT BallPrediction = new();

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
                    + "manually, e.g. `RLBOT_AGENT_ID=<agent_id> python yourbot.py`"
            );

            throw new Exception(
                "Environment variable RLBOT_AGENT_ID is not set and no default agent id is passed to the constructor of the bot."
            );
        }

        Logger = new Logging("Bot", LogLevel.Information);
        _gameInterface = new Interface(agentId, logger: Logger);
        _gameInterface.MatchSettingsHandlers.Add(_handle_match_settings);
        _gameInterface.FieldInfoHandlers.Add(_handle_field_info);
        _gameInterface.MatchCommunicationHandlers.Add(_handle_match_communication);
        _gameInterface.BallPredictionHandlers.Add(_handle_ball_prediction);
        _gameInterface.ControllableTeamInfoHandlers.Add(_handle_controllable_team_info);
        _gameInterface.GamePacketHandlers.Add(_handle_game_tick_packet);
    }

    private void _try_initialize()
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
            throw;
        }

        _initializedBot = true;
        _gameInterface.SendInitComplete();
    }

    public virtual void Initialize() { }

    private void _handle_match_settings(MatchSettingsT matchSettings)
    {
        MatchSettings = matchSettings;
        _hasMatchSettings = true;
        _try_initialize();
    }

    private void _handle_field_info(FieldInfoT fieldInfo)
    {
        FieldInfo = fieldInfo;
        _hasFieldInfo = true;
        _try_initialize();
    }

    private void _handle_match_communication(MatchCommT matchComm)
    {
        Logger.LogInformation("Match communication received");
    }

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

    private void _handle_ball_prediction(BallPredictionT ballPrediction)
    {
        _latestPrediction = ballPrediction;
    }

    private void _handle_controllable_team_info(ControllableTeamInfoT controllableTeamInfo)
    {
        Team = (int)controllableTeamInfo.Team;
        var controllable = controllableTeamInfo.Controllables[0];
        Index = (int)controllable.Index;
        SpawnId = controllable.SpawnId;
        _hasPlayerMapping = true;

        _try_initialize();
    }

    private void _handle_game_tick_packet(GamePacketT gamePacket)
    {
        _latestPacket = gamePacket;
    }

    private void _packetProcessor(GamePacketT packet)
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
                    _packetProcessor(_latestPacket);
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

    public virtual void Retire() { }

    public abstract ControllerStateT GetOutput(GamePacketT GamePacket);
}
