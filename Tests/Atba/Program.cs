using rlbot.flat;

Atba bot = new();
bot.Run();

class Atba : Bot
{
    public Atba()
        : base("community/csharp_atba") { }

    public override ControllerStateT GetOutput(GamePacketT GamePacket)
    {
        ControllerStateT controller = new();
        controller.Throttle = 1;
        controller.Boost = true;
        controller.Steer = 0.1f;

        return controller;
    }
}
