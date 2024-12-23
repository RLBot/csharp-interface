using RLBot.Flat;

namespace MyBot.Math;

public struct Vec2
{
    public float X;
    public float Y;

    public Vec2(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }

    public Vec2(Vector3T vec)
    {
        X = vec.X;
        Y = vec.Y;
    }

    public static Vec2 operator +(Vec2 a, Vec2 b)
    {
        return new Vec2(a.X + b.X, a.Y + b.Y);
    }

    public static Vec2 operator -(Vec2 a, Vec2 b)
    {
        return new Vec2(a.X - b.X, a.Y - b.Y);
    }

    public float SteerTo(Vec2 ideal)
    {
        // The in-game axes are left-handed, so use -x
        float currentInRadians = MathF.Atan2(Y, -X);
        float idealInRadians = MathF.Atan2(ideal.Y, -ideal.X);

        float correction = idealInRadians - currentInRadians;

        if (MathF.Abs(correction) > MathF.PI)
        {
            if (correction < 0)
                correction += 2 * MathF.PI;
            else
                correction -= 2 * MathF.PI;
        }

        return -correction;
    }
}

public static class PlayerExtensions
{
    public static Vec2 GetCarFacingVector(this PlayerInfoT car)
    {
        float pitch = car.Physics.Rotation.Pitch;
        float yaw = car.Physics.Rotation.Yaw;

        float facingX = MathF.Cos(pitch) * MathF.Cos(yaw);
        float facingY = MathF.Cos(pitch) * MathF.Sin(yaw);

        return new Vec2(facingX, facingY);
    }
}
