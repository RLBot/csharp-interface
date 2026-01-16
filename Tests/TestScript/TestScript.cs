using Microsoft.Extensions.Logging;
using RLBot;
using RLBot.Flat;
using RLBot.Manager;
using RLBot.Util;
using Vector3 = System.Numerics.Vector3;

var manager = new ScriptManager(
    new RLBotInterface(),
    "test/csharp_script",
    scriptParams => new TestScript(scriptParams)
);
manager.Run();

class TestScript : AbstractScript
{
    private readonly Logging _logger = new Logging(nameof(TestScript), LogLevel.Information);

    public readonly Renderer Renderer;

    private float _next = 10f;

    public TestScript(ScriptInitParams scriptParams) : base(scriptParams)
    {
        Renderer = new Renderer(Rlbot);

        _logger.LogInformation("Test script initialized!");
    }

    public override void ProcessPacket(GamePacketT packet, BallPredictionT? ballPred)
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

    public override void OnMatchCommReceived(MatchCommT msg) { }

    public override void OnRetire() { }
}
