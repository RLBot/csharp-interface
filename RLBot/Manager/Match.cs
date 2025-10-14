using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBot.GameState;
using RLBot.Util;

namespace RLBot.Manager;

public class Match
{
    private readonly Logging _logger = new Logging("Match", LogLevel.Information);

    public GamePacketT? Packet { get; private set; }
    private Process? _rlbotServerProcess;
    private int _rlbotServerPort = RLBotInterface.DEFAULT_RLBOT_SERVER_PORT;
    private bool _initialized = false;

    private string? _mainExecutablePath;
    private string _mainExecutableName = OsConstants.MainExecutableName;

    private RLBotInterface _rlbotInterface;

    public Match(
        string? mainExecutablePath = null,
        string? mainExecutableName = null
    )
    {
        _mainExecutablePath = mainExecutablePath;
        if (mainExecutableName != null)
            _mainExecutableName = mainExecutableName;

        _rlbotInterface = new RLBotInterface("", logger: _logger);
        _rlbotInterface.OnGamePacketCallback += PacketReporter;
    }

    public void EnsureServerStarted()
    {
        _logger.LogWarning("The C# RLBot Interface cannot ensure the RLBot Server is running. The feature has yet to be implemented.");
        
        // self.rlbot_server_process, self.rlbot_server_port = gateway.find_server_process(
        //     self.main_executable_name
        // )

        if (_rlbotServerProcess != null)
        {
            _logger.LogInformation("Already have {0} running!", _mainExecutableName);
            return;
        }

        if (_mainExecutablePath == null)
            _mainExecutablePath = Directory.GetCurrentDirectory();

        // rlbot_server_process, self.rlbot_server_port = gateway.launch(
        //     self.main_executable_path,
        //     self.main_executable_name,
        // )
        // self.rlbot_server_process = psutil.Process(rlbot_server_process.pid)

        // self.logger.info(
        //     "Started %s with process id %s",
        //     self.main_executable_name,
        //     self.rlbot_server_process.pid,
        // )
    }

    private void PacketReporter(GamePacketT packet) => Packet = packet;

    public void Connect(
        bool wantsMatchCommunications,
        bool wantsBallPredictions,
        bool closeAfterMatch = true,
        int rlbotServerPort = RLBotInterface.DEFAULT_RLBOT_SERVER_PORT
    ) =>
        _rlbotInterface.Connect(
            wantsMatchCommunications,
            wantsBallPredictions,
            closeAfterMatch,
            rlbotServerPort
        );

    public void WaitForFirstPacket()
    {
        while (
            Packet == null
            || Packet.MatchInfo.MatchPhase == MatchPhase.Inactive
            || Packet.MatchInfo.MatchPhase == MatchPhase.Ended
        )
            Thread.Sleep(100);
    }

    public void StartMatch(MatchConfigurationT config, bool waitForStart = true)
    {
        EnsureGameConnection();

        _rlbotInterface.StartMatch(config);

        if (!_initialized)
        {
            _rlbotInterface.SendInitComplete();
            _initialized = true;
        }

        if (waitForStart)
        {
            WaitForFirstPacket();
            _logger.LogInformation("Match has started.");
        }
    }

    public void StartMatch(string configPath, bool waitForStart = true)
    {
        EnsureGameConnection();

        _rlbotInterface.StartMatch(configPath);

        if (!_initialized)
        {
            _rlbotInterface.SendInitComplete();
            _initialized = true;
        }

        if (waitForStart)
        {
            WaitForFirstPacket();
            _logger.LogInformation("Match has started.");
        }
    }

    private void EnsureGameConnection()
    {
        if (!_rlbotInterface.IsConnected)
        {
            _rlbotInterface.Connect(
                wantsMatchCommunications: false,
                wantsBallPredictions: false,
                closeBetweenMatches: false
            );
            _rlbotInterface.Run(backgroundThread: true);
        }
    }

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

    public void Disconnect() => _rlbotInterface.Disconnect();

    public void StopMatch() => _rlbotInterface.StopMatch();
}
