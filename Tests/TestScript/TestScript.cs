using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBot.Manager;

TestScript script = new TestScript();
script.Run();

class TestScript : Script
{
    private float _next = 10f;

    public TestScript()
        : base("test/csharp_script") { }

    public override void Initialize()
    {
        Logger.LogInformation("Test script initialized!");
    }

    public override void HandlePacket(GamePacketT packet)
    {
        if (packet.GameInfo.SecondsElapsed < _next || packet.Balls.Count == 0)
            return;

        Dictionary<int, DesiredBallStateT> balls = new();
        DesiredBallStateT ball = new DesiredBallStateT();
        ball.Physics = new DesiredPhysicsT();
        ball.Physics.Velocity = new Vector3PartialT();
        ball.Physics.Velocity.Y = new FloatT();
        ball.Physics.Velocity.Y.Val = packet.Balls[0].Physics.Velocity.Y + 400f;
        balls.Add(0, ball);
        SetGameState(balls: balls);

        _next = packet.GameInfo.SecondsElapsed + 10f;
    }
}
