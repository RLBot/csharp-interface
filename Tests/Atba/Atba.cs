using Microsoft.Extensions.Logging;
using MyBot.Math;
using RLBot;
using RLBot.Flat;
using RLBot.Manager;
using RLBot.Util;

RLBotInterface rlbot = new();
SingleThreadBotManager manager = new(
    rlbot,
    "test/csharp_atba",
    botParams => new Atba(botParams)
);
manager.Run();

internal class Atba : AbstractBot
{
    private readonly Logging _logger = new Logging(nameof(Atba), LogLevel.Information);

    public readonly Renderer Renderer;

    public Atba(BotInitParams botParams)
        : base(botParams)
    {
        _logger.LogInformation("Initializing agent!");
        Renderer = new(Rlbot);

        _logger.LogInformation(
            $"There are {FieldInfo.BoostPads.Count} boost pads on the field."
        );
    }

    public override PlayerLoadoutT? GetInitialLoadout()
    {
        return null; // Use the loadout declared in bot.toml
    }

    public override ControllerStateT GetOutput(
        GamePacketT packet,
        BallPredictionT? ballPrediction
    )
    {
        ControllerStateT controller = new();

        if (packet.Balls.Count == 0)
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
        Renderer.DrawLine3D(
            myCar.Physics.Location.ToSysVec(),
            packet.Balls[0].Physics.Location.ToSysVec()
        );
        Renderer.End();

        return controller;
    }

    public override void OnMatchCommReceived(MatchCommT msg) { }

    public override void OnRetire() { }
}
