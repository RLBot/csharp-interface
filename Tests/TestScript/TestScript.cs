using Microsoft.Extensions.Logging;
using RLBot.Flat;
using Vector3 = System.Numerics.Vector3;
using RLBot.Manager;

TestScript script = new TestScript();
script.Run();

class TestScript : Script
{
    private float _next = 10f;

    public TestScript()
        : base("test/csharp_script")
    {
    }

    public override void Initialize()
    {
        Logger.LogInformation("Test script initialized!");
    }

    public override void HandlePacket(GamePacketT packet)
    {
        if (packet.MatchInfo.SecondsElapsed < _next || packet.Balls.Count == 0)
            return;

        // State setting
        GameStateBuilder()
            .Balls(Enumerable.Range(0, packet.Balls.Count), (i, c) => c
                .Location(Vector3.UnitZ * 93)
                .VelocityZ(packet.Balls[i].Physics.Velocity.Z + 1000f)
            )
            .Car(1, c => c.Boost(100).RotationYaw(0))
            .BuildAndSend();

        _next = packet.MatchInfo.SecondsElapsed + 10f;
    }
}
