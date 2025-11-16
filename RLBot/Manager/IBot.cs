using RLBot.Flat;

namespace RLBot.Manager;

public interface IBot
{
    PlayerLoadoutT? GetInitialLoadout();

    ControllerStateT? GetOutput(GamePacketT packet, BallPredictionT? ballPrediction);

    void OnMatchCommReceived(MatchCommT comm);

    void OnRetire();
}
