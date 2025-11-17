using RLBot.Flat;

namespace RLBot.Manager;

public interface IScript
{
    void ProcessPacket(GamePacketT gamePacket, BallPredictionT? ballPrediction);

    void OnMatchCommReceived(MatchCommT msg);

    void OnRetire();
}
