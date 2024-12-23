using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBot.Util;

namespace RLBot;

public class Interface
{
    private bool _isConnected = false;
    private bool _running = false;
    private FlatBufferBuilder _flatBufferBuilder = new(1024);

    private readonly int _connectionTimeout;
    private readonly Logging _logger;
    private readonly TcpClient _client = new();
    private SocketSpecStreamReader? _socketSpecReader;
    private SocketSpecStreamWriter? _socketSpecWriter;

    public readonly string AgentId;
    public event Action OnConnectCallback = delegate { };
    public event Action<GamePacketT> OnGamePacketCallback = delegate { };
    public event Action<FieldInfoT> OnFieldInfoCallback = delegate { };
    public event Action<MatchSettingsT> OnMatchSettingsCallback = delegate { };
    public event Action<MatchCommT> OnMatchCommunicationCallback = delegate { };
    public event Action<BallPredictionT> OnBallPredictionCallback = delegate { };
    public event Action<ControllableTeamInfoT> OnControllableTeamInfoCallback = delegate { };
    public event Action<TypedPayload> OnRawPayloadCallback = delegate { };

    public Interface(string agentId, int connectionTimeout = 120, Logging? logger = null)
    {
        AgentId = agentId;
        _connectionTimeout = connectionTimeout;

        if (logger is null)
        {
            _logger = new Logging("Interface", LogLevel.Information);
        }
        else
        {
            _logger = logger;
        }

        _client.NoDelay = true;
    }

    public void SendFlatBuffer<T>(DataType type, Offset<T> offset)
        where T : struct
    {
        if (!_isConnected)
        {
            throw new Exception("Connection has not been established");
        }
        _flatBufferBuilder.Finish(offset.Value);
        _socketSpecWriter!.Write(TypedPayload.FromFlatBufferBuilder(type, _flatBufferBuilder));
        _socketSpecWriter.Send();
    }

    public void SendInitComplete()
    {
        if (!_isConnected)
        {
            throw new Exception("Connection has not been established");
        }
        _socketSpecWriter!.Write(
            new TypedPayload()
            {
                Type = DataType.InitComplete,
                Payload = new ArraySegment<byte>(Array.Empty<byte>()),
            }
        );
        _socketSpecWriter.Send();
    }

    public void SendSetLoadout(SetLoadoutT setLoadout)
    {
        _flatBufferBuilder.Clear();
        var offset = SetLoadout.Pack(_flatBufferBuilder, setLoadout);
        SendFlatBuffer(DataType.SetLoadout, offset);
    }

    public void SendMatchComm(MatchCommT matchComm)
    {
        _flatBufferBuilder.Clear();
        var offset = MatchComm.Pack(_flatBufferBuilder, matchComm);
        SendFlatBuffer(DataType.MatchComms, offset);
    }

    public void SendPlayerInput(PlayerInputT playerInput)
    {
        _flatBufferBuilder.Clear();
        var offset = PlayerInput.Pack(_flatBufferBuilder, playerInput);
        SendFlatBuffer(DataType.PlayerInput, offset);
    }

    public void SendGameState(DesiredGameStateT gameState)
    {
        _flatBufferBuilder.Clear();
        var offset = DesiredGameState.Pack(_flatBufferBuilder, gameState);
        SendFlatBuffer(DataType.DesiredGameState, offset);
    }

    public void SendRenderGroup(RenderGroupT renderGroup)
    {
        _flatBufferBuilder.Clear();
        var offset = RenderGroup.Pack(_flatBufferBuilder, renderGroup);
        SendFlatBuffer(DataType.RenderGroup, offset);
    }

    public void SendRemoveRenderGroup(RemoveRenderGroupT removeRenderGroup)
    {
        _flatBufferBuilder.Clear();
        var offset = RemoveRenderGroup.Pack(_flatBufferBuilder, removeRenderGroup);
        SendFlatBuffer(DataType.RemoveRenderGroup, offset);
    }

    public void StopMatch(bool shutdownServer = false)
    {
        var stopCommand = new StopCommandT { ShutdownServer = shutdownServer };

        _flatBufferBuilder.Clear();
        var offset = StopCommand.Pack(_flatBufferBuilder, stopCommand);
        SendFlatBuffer(DataType.StopCommand, offset);
    }

    public void StartMatch(MatchSettingsT matchSettings)
    {
        _flatBufferBuilder.Clear();
        var offset = MatchSettings.Pack(_flatBufferBuilder, matchSettings);
        SendFlatBuffer(DataType.MatchSettings, offset);
    }

    public void StartMatch(string matchSettingsPath)
    {
        var startCommand = new StartCommandT { ConfigPath = matchSettingsPath };

        _flatBufferBuilder.Clear();
        var offset = StartCommand.Pack(_flatBufferBuilder, startCommand);
        SendFlatBuffer(DataType.StartCommand, offset);
    }

    public void Connect(
        bool wantsMatchCommunications,
        bool wantsBallPredictions,
        bool closeAfterMatch = true,
        int rlbotServerPort = 23234
    )
    {
        if (_isConnected)
        {
            throw new Exception("Connection has already been established");
        }

        _client.ReceiveTimeout = _connectionTimeout * 1000;

        try
        {
            var beginTime = DateTime.Now;
            var nextWarning = 10;
            while (DateTime.Now < beginTime.AddSeconds(_connectionTimeout))
            {
                try
                {
                    _client.Connect(new IPAddress([127, 0, 0, 1]), rlbotServerPort);
                    _isConnected = true;
                    break;
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        Thread.Sleep(100);
                    }
                    else if (e.SocketErrorCode == SocketError.ConnectionAborted)
                    {
                        Thread.Sleep(100);
                    }

                    if (DateTime.Now > beginTime.AddSeconds(nextWarning))
                    {
                        nextWarning *= 2;
                        _logger.LogWarning(
                            "Connection is being refused/aborted. Trying again ..."
                        );
                    }
                }
            }

            if (!_isConnected)
            {
                throw new SocketException(
                    (int)SocketError.ConnectionRefused,
                    "Connection was refused/aborted repeatedly! "
                        + "Ensure that Rocket League and the RLBotServer is running. "
                        + "Try calling `ensure_server_started()` before connecting."
                );
            }
        }
        catch (TimeoutException e)
        {
            throw new TimeoutException(
                "Took too long to connect to the RLBot! "
                    + "Ensure that Rocket League and the RLBotServer is running."
                    + "Try calling `ensure_server_started()` before connecting.",
                e
            );
        }
        finally
        {
            _client.ReceiveTimeout = 0;
        }

        _socketSpecReader = new SocketSpecStreamReader(_client.GetStream());
        _socketSpecWriter = new SocketSpecStreamWriter(_client.GetStream());

        IPEndPoint? localIpEndPoint = _client.Client.LocalEndPoint as IPEndPoint;
        _logger.LogInformation(
            "Connected to port {0} from port {1}!",
            rlbotServerPort,
            localIpEndPoint!.Port
        );

        OnConnectCallback();

        var flatbuffer = new ConnectionSettingsT
        {
            AgentId = AgentId,
            WantsBallPredictions = wantsBallPredictions,
            WantsComms = wantsMatchCommunications,
            CloseAfterMatch = closeAfterMatch,
        };
        _flatBufferBuilder.Clear();
        int offset = ConnectionSettings.Pack(_flatBufferBuilder, flatbuffer).Value;
        _flatBufferBuilder.Finish(offset);

        _socketSpecWriter.Write(
            TypedPayload.FromFlatBufferBuilder(DataType.ConnectionSettings, _flatBufferBuilder)
        );
        _socketSpecWriter.Send();
    }

    public bool HandleIncomingMessages(bool blocking = false)
    {
        if (!_isConnected)
        {
            throw new Exception("Connection has not been established");
        }

        try
        {
            _client.Client.Blocking = blocking;

            TypedPayload incomingMessage = _socketSpecReader!.ReadOne();

            try
            {
                return HandleIncomingMessage(incomingMessage);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Unexpected error while handling message of type {0}: {1}",
                    incomingMessage.Type,
                    e
                );
                return false;
            }
        }
        catch (SocketException)
        {
            return true;
        }
        catch
        {
            _logger.LogError("SocketRelay disconnected unexpectedly!");
            return false;
        }
    }

    private bool HandleIncomingMessage(TypedPayload msg)
    {
        OnRawPayloadCallback(msg);

        ByteBuffer byteBuffer = new(msg.Payload.Array, msg.Payload.Offset);

        switch (msg.Type)
        {
            case DataType.None:
                return false;
            case DataType.GamePacket:
                GamePacketT gamePacket = GamePacket.GetRootAsGamePacket(byteBuffer).UnPack();
                OnGamePacketCallback(gamePacket);
                break;
            case DataType.FieldInfo:
                FieldInfoT fieldInfo = FieldInfo.GetRootAsFieldInfo(byteBuffer).UnPack();
                OnFieldInfoCallback(fieldInfo);
                break;
            case DataType.MatchSettings:
                MatchSettingsT matchSettings = MatchSettings
                    .GetRootAsMatchSettings(byteBuffer)
                    .UnPack();
                OnMatchSettingsCallback(matchSettings);
                break;
            case DataType.MatchComms:
                MatchCommT matchComm = MatchComm.GetRootAsMatchComm(byteBuffer).UnPack();
                OnMatchCommunicationCallback(matchComm);
                break;
            case DataType.BallPrediction:
                BallPredictionT ballPrediction = BallPrediction
                    .GetRootAsBallPrediction(byteBuffer)
                    .UnPack();
                OnBallPredictionCallback(ballPrediction);
                break;
            case DataType.ControllableTeamInfo:
                ControllableTeamInfoT controllableTeamInfo = ControllableTeamInfo
                    .GetRootAsControllableTeamInfo(byteBuffer)
                    .UnPack();
                OnControllableTeamInfoCallback(controllableTeamInfo);
                break;
            default:
                _logger.LogWarning("Received message of unknown type: {0}", msg.Type);
                break;
        }

        return true;
    }

    public void Disconnect()
    {
        if (!_isConnected)
        {
            _logger.LogWarning("Asked to disconnect but was already disconnected.");
            return;
        }

        _socketSpecWriter!.Write(
            new TypedPayload()
            {
                Type = DataType.None,
                Payload = new ArraySegment<byte>(Array.Empty<byte>()),
            }
        );
        _socketSpecWriter.Send();

        var timeout = 5.0;
        while (_running && timeout > 0)
        {
            Thread.Sleep(100);
            timeout -= 0.1;
        }

        if (timeout <= 0)
        {
            _logger.LogCritical("RLBot is not responding to our disconnect request!?");
            _running = false;
        }

        Debug.Assert(
            !_running,
            "Disconnect request or timeout should have set _running to False"
        );

        _isConnected = false;
    }
}
