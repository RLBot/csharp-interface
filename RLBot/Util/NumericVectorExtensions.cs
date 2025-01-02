using RLBot.Flat;

namespace RLBot.Util;

public static class NumericVectorExtensions
{
    public static Vector2T ToFlatBuf(this System.Numerics.Vector2 v) => new() { X = v.X, Y = v.Y };

    public static Vector3T ToFlatBuf(this System.Numerics.Vector3 v) => new() { X = v.X, Y = v.Y, Z = v.Z };
}
