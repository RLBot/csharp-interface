using Microsoft.Extensions.Logging;
using MyBot.Math;
using RLBot;
using RLBot.Flat;
using RLBot.Manager;
using RLBot.Util;
using Color = System.Drawing.Color;
using Vector3 = System.Numerics.Vector3;

RLBotInterface rlbot = new();
SingleThreadBotManager manager = new(
    rlbot,
    "test/csharp_atba",
    (rlbot, index, team, name, agentId, matchConfig, fieldInfo) =>
        new Atba(rlbot, index, team, name, agentId, matchConfig, fieldInfo)
);
manager.Run();

internal class Atba : IBot
{
    private readonly Logging _logger = new Logging(nameof(Atba), LogLevel.Information);

    public readonly RLBotInterface Rlbot;
    public readonly int Index;
    public readonly uint Team;
    public readonly string Name;
    public readonly string AgentId;
    public readonly MatchConfigurationT MatchConfig;
    public readonly FieldInfoT FieldInfo;

    public readonly Renderer Renderer;

    public Atba(
        RLBotInterface rlbot,
        int index,
        uint team,
        string name,
        string agentId,
        MatchConfigurationT matchConfig,
        FieldInfoT fieldInfo
    )
    {
        Rlbot = rlbot;
        Index = index;
        Team = team;
        Name = name;
        AgentId = agentId;
        MatchConfig = matchConfig;
        FieldInfo = fieldInfo;
        Renderer = new(rlbot);

        _logger.LogInformation("Initializing agent!");

        int numBoostPads = fieldInfo.BoostPads.Count;
        _logger.LogInformation($"There are {numBoostPads} boost pads on the field.");
    }

    public PlayerLoadoutT? GetInitialLoadout()
    {
        return null; // Use the loadout declared in bot.toml
    }

    public ControllerStateT GetOutput(GamePacketT packet, BallPredictionT? ballPrediction)
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

    public void OnMatchCommReceived(MatchCommT comm) { }

    public void OnRetire() { }
}
