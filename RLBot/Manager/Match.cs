using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBot.Util;

namespace RLBot.Manager;

public class Match
{
    private readonly Logging _logger = new Logging("Match", LogLevel.Information);

    public GamePacketT? Packet { get; private set; }
    private Process? _rlbotServerProcess;
    private int _rlbotServerPort = Interface.RLBOT_SERVER_PORT;
    private bool _initialized = false;

    private string? _mainExecutablePath;
    private string _mainExecutableName = OsConstants.MainExecutableName;

    private Interface _gameInterface;

    public Match(
        string? mainExecutablePath = null,
        string? mainExecutableName = null,
        bool printVersionInfo = true
    )
    {
        _mainExecutablePath = mainExecutablePath;
        if (mainExecutableName != null)
            _mainExecutableName = mainExecutableName;

        _gameInterface = new Interface("", logger: _logger);
        _gameInterface.OnGamePacketCallback += PacketReporter;

        if (printVersionInfo)
            Version.PrintCurrentReleaseNotes();
    }

    public void EnsureServerStarted()
    {
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
        int rlbotServerPort = Interface.RLBOT_SERVER_PORT
    ) =>
        _gameInterface.Connect(
            wantsMatchCommunications,
            wantsBallPredictions,
            closeAfterMatch,
            rlbotServerPort
        );

    public void WaitForFirstPacket()
    {
        while (
            Packet == null
            || Packet.GameInfo.GameStatus == GameStatus.Inactive
            || Packet.GameInfo.GameStatus == GameStatus.Ended
        )
            Thread.Sleep(100);
    }

    public void StartMatch(MatchSettingsT settings, bool waitForStart = true)
    {
        EnsureGameConnection();

        _gameInterface.StartMatch(settings);

        if (!_initialized)
        {
            _gameInterface.SendInitComplete();
            _initialized = true;
        }

        if (waitForStart)
        {
            WaitForFirstPacket();
            _logger.LogInformation("Match has started.");
        }
    }

    public void StartMatch(string settings, bool waitForStart = true)
    {
        EnsureGameConnection();

        _gameInterface.StartMatch(settings);

        if (!_initialized)
        {
            _gameInterface.SendInitComplete();
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
        if (!_gameInterface.IsConnected)
        {
            _gameInterface.Connect(
                wantsMatchCommunications: false,
                wantsBallPredictions: false,
                closeAfterMatch: false
            );
            _gameInterface.Run(backgroundThread: true);
        }
    }

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

    public void Disconnect() => _gameInterface.Disconnect();

    public void StopMatch() => _gameInterface.StopMatch();
}
