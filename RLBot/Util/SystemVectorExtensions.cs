using RLBot.Flat;

namespace RLBot.Util;

/// <summary>Static class with extension methods for vectors.</summary>
public static class SystemVectorExtensions
{
    /// <summary>Convert a system vector to a flatbuffer vector.</summary>
    public static Vector2T ToFlatBuf(this System.Numerics.Vector2 v) => new() { X = v.X, Y = v.Y };

    /// <summary>Convert a system vector to a flatbuffer vector.</summary>
    public static Vector3T ToFlatBuf(this System.Numerics.Vector3 v) => new() { X = v.X, Y = v.Y, Z = v.Z };
}
