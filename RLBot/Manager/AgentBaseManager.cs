using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBot.Util;

namespace RLBot.Manager;

public abstract class AgentBaseManager
{
    protected Logging Logger = new(nameof(AgentBaseManager), LogLevel.Information);

    public RLBotInterface Rlbot { get; }
    public string AgentId { get; }

    public ControllableTeamInfoT TeamInfo { get; private set; } = new();
    public MatchConfigurationT MatchConfig { get; private set; } = new();
    public FieldInfoT FieldInfo { get; private set; } = new();
    public bool IsInitialized { get; private set; } = false;

    public bool HasMatchConfig { get; private set; } = false;
    public bool HasFieldInfo { get; private set; } = false;
    public bool HasTeamInfo { get; private set; } = false;

    protected GamePacketT? _latestPacket;
    protected BallPredictionT? _latestPrediction;

    public AgentBaseManager(RLBotInterface rlbot, string? defaultAgentId = null)
    {
        var aid = Environment.GetEnvironmentVariable("RLBOT_AGENT_ID") ?? defaultAgentId;

        if (aid is null)
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

        AgentId = aid;

        Rlbot = rlbot;
        Rlbot.OnMatchConfigCallback += HandleMatchConfig;
        Rlbot.OnFieldInfoCallback += HandleFieldInfo;
        Rlbot.OnMatchCommunicationCallback += HandleMatchComm;
        Rlbot.OnBallPredictionCallback += HandleBallPrediction;
        Rlbot.OnControllableTeamInfoCallback += HandleControllableTeamInfo;
        Rlbot.OnGamePacketCallback += HandleGamePacket;
    }

    private void TryInitialize()
    {
        if (IsInitialized || !HasMatchConfig || !HasFieldInfo || !HasTeamInfo)
            return;

        try
        {
            Logger.LogDebug("Initializing agent: {}", AgentId);
            Initialize();
        }
        catch (Exception e)
        {
            Logger.LogCritical("Failed to initialize agent: {}.\n{}", AgentId, e);
            return;
        }

        Rlbot.SendInitComplete();
        IsInitialized = true;
    }

    protected abstract void Initialize();

    private void HandleMatchConfig(MatchConfigurationT matchConfig)
    {
        MatchConfig = matchConfig;
        HasMatchConfig = true;
        TryInitialize();
    }

    private void HandleFieldInfo(FieldInfoT fieldInfo)
    {
        FieldInfo = fieldInfo;
        HasFieldInfo = true;
        TryInitialize();
    }

    protected abstract void HandleMatchComm(MatchCommT matchComm);

    public void SendMatchComm(
        int index,
        int team,
        List<byte> content,
        string? display = null,
        bool teamOnly = false
    )
    {
        Rlbot.SendMatchComm(
            new MatchCommT
            {
                Index = (uint)index,
                Team = (uint)team,
                Content = content,
                Display = display,
                TeamOnly = teamOnly,
            }
        );
    }

    private void HandleBallPrediction(BallPredictionT ballPrediction) =>
        _latestPrediction = ballPrediction;

    private void HandleControllableTeamInfo(ControllableTeamInfoT controllableTeamInfo)
    {
        TeamInfo = controllableTeamInfo;
        HasTeamInfo = true;
        TryInitialize();
    }

    private void HandleGamePacket(GamePacketT gamePacket) => _latestPacket = gamePacket;

    public void Run(bool wantsMatchCommunications = true, bool wantsBallPredictions = true)
    {
        try
        {
            Rlbot.Connect(AgentId, wantsMatchCommunications, wantsBallPredictions, false);

            while (true)
            {
                var res = Rlbot.HandleNextIncomingMessage(blocking: _latestPacket is null);

                switch (res)
                {
                    case RLBotInterface.MsgHandlingResult.Terminated:
                        return;
                    case RLBotInterface.MsgHandlingResult.MoreMsgsQueued:
                        continue;
                    case RLBotInterface.MsgHandlingResult.NoIncomingMsgs:
                        if (_latestPacket is not null)
                        {
                            ProcessPacket();
                            _latestPacket = null;
                        }
                        continue;
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

    protected abstract void ProcessPacket();

    protected abstract void Retire();
}
