using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBot.Util;

namespace RLBot.Manager;

/// <summary>
/// The AgentBaseManager class is an abstract base class for managing agents in RLBot and
/// implements the common initialization and packet reading behavior. That is, the
/// AgentBaseManager will wait for the match configuration, field information, and team information
/// before initializing the agent(s) of this process. Once initialization is complete,
/// the AgentBaseManager handles messages from RLBot discarding any outdated game packets,
/// finally passing the latest packet and ball prediction to the implementer.
/// </summary>
/// <seealso cref="SingleThreadBotManager"/>
/// <seealso cref="ScriptManager"/>
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

    /// <summary>Initialize the agents of this process if the all required information has been received.</summary>
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

    /// <summary>
    /// Called when all required information is ready.
    /// Bot managers should send initial loadouts before returning.
    /// </summary>
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

    protected abstract void HandleMatchComm(MatchCommT msg);

    private void HandleBallPrediction(BallPredictionT ballPrediction) =>
        _latestPrediction = ballPrediction;

    private void HandleControllableTeamInfo(ControllableTeamInfoT controllableTeamInfo)
    {
        TeamInfo = controllableTeamInfo;
        HasTeamInfo = true;
        TryInitialize();
    }

    private void HandleGamePacket(GamePacketT gamePacket) => _latestPacket = gamePacket;

    /// <summary>
    /// Run the agent manager. This will connect to RLBot and start reading packages. Blocking.
    /// </summary>
    /// <param name="wantsMatchCommunications">Whether this process wants to receive match comms.</param>
    /// <param name="wantsBallPredictions">Whether this process wants to receive ball prediction.</param>
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

    /// <summary>
    /// Process the latest game packet and ball prediction.
    /// See <see cref="_latestPacket"/> and <see cref="_latestPrediction"/>.
    /// Ball prediction may be null, if ball prediction was not requested.
    /// </summary>
    protected abstract void ProcessPacket();

    /// <summary>
    /// Invoked when the agent shuts down.
    /// </summary>
    protected abstract void Retire();
}
