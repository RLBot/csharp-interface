using RLBot.Flat;

namespace RLBot.Util;

public static class RenderAnchorExtensions
{
    public static RenderAnchorT ToRenderAnchor(this CarAnchorT anchor) =>
        new() { Relative = RelativeAnchorUnion.FromCarAnchor(anchor) };

    public static RenderAnchorT ToRenderAnchor(this BallAnchorT anchor) =>
        new() { Relative = RelativeAnchorUnion.FromBallAnchor(anchor) };
}
