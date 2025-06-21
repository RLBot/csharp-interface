using Microsoft.Extensions.Logging;
using MyBot.Math;
using RLBot.Flat;
using RLBot.Manager;
using RLBot.Util;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;

Atba bot = new();
bot.Run();

class Atba : Bot
{
    public Atba()
        : base("test/csharp_atba") { }

    public override void Initialize()
    {
        Logger.LogInformation("Initializing agent!");

        int numBoostPads = FieldInfo.BoostPads.Count;
        Logger.LogInformation($"There are {numBoostPads} boost pads on the field.");
    }

    public override ControllerStateT GetOutput(GamePacketT packet)
    {
        ControllerStateT controller = new();

        if (
            packet.MatchInfo.MatchPhase != MatchPhase.Active
                && packet.MatchInfo.MatchPhase != MatchPhase.Kickoff
            || packet.Balls.Count == 0
        )
            return controller;

        Vec2 ballLocation = new(packet.Balls[0].Physics.Location);

        PlayerInfoT myCar = packet.Players[Index];
        Vec2 carLocation = new(myCar.Physics.Location);
        Vec2 carDirection = myCar.GetCarFacingVector();
        Vec2 carToBall = ballLocation - carLocation;

        float steerCorrection = carDirection.SteerTo(carToBall);

        controller.Steer = steerCorrection;
        controller.Throttle = 1;

        controller.Jump = packet.MatchInfo.LastSpectated == Index;
        
        Renderer.Begin();
        Renderer.DrawLine3D(myCar.Physics.Location.ToSysVec(), packet.Balls[0].Physics.Location.ToSysVec(), Color.White);
        Renderer.End();

        return controller;
    }
}
