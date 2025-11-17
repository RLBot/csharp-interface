using Microsoft.Extensions.Logging;
using RLBot;
using RLBot.Flat;
using RLBot.Manager;
using RLBot.Util;
using Vector3 = System.Numerics.Vector3;

var manager = new ScriptManager(
    new RLBotInterface(),
    "test/csharp_script",
    (rlbot, index, agentId, matchConfig, fieldInfo) =>
        new TestScript(rlbot, index, agentId, matchConfig, fieldInfo)
);
manager.Run();

class TestScript : IScript
{
    private readonly Logging _logger = new Logging(nameof(TestScript), LogLevel.Information);

    public readonly RLBotInterface Rlbot;
    public readonly int Index;
    public readonly string AgentId;
    public readonly MatchConfigurationT MatchConfig;
    public readonly FieldInfoT FieldInfo;

    public readonly Renderer Renderer;

    private float _next = 10f;

    public TestScript(
        RLBotInterface rlbot,
        int index,
        string agentId,
        MatchConfigurationT matchConfig,
        FieldInfoT fieldInfo
    )
    {
        Rlbot = rlbot;
        Index = index;
        AgentId = agentId;
        MatchConfig = matchConfig;
        FieldInfo = fieldInfo;
        Renderer = new Renderer(rlbot);

        _logger.LogInformation("Test script initialized!");
    }

    public void ProcessPacket(GamePacketT packet, BallPredictionT? ballPred)
    {
        if (
            packet.MatchInfo.SecondsElapsed < _next
            || packet.Balls.Count == 0
            || packet.Players.Count == 0
        )
            return;

        // Test state setting
        Rlbot
            .GameStateBuilder()
            .Balls(
                Enumerable.Range(0, packet.Balls.Count),
                (i, c) =>
                    c.Location(Vector3.UnitZ * 93)
                        .VelocityZ(packet.Balls[i].Physics.Velocity.Z + 1000f)
            )
            .Car(1, c => c.Boost(100).RotationYaw(0))
            .BuildAndSend();

        _next = packet.MatchInfo.SecondsElapsed + 10f;
    }

    public void OnMatchCommReceived(MatchCommT msg) { }

    public void OnRetire() { }
}
