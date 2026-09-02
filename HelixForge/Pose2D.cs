using System;

namespace HelixForge;

/// <summary>
/// Immutable 2D pose (position and heading) on a planar surface.
/// X/Y in meters, Yaw in radians.
/// </summary>
public readonly struct Pose2D : IEquatable<Pose2D>
{
    /// <summary>Position along the X axis in meters.</summary>
    public double X { get; }

    /// <summary>Position along the Y axis in meters.</summary>
    public double Y { get; }

    /// <summary>Heading (yaw) in radians.</summary>
    public double Yaw { get; }

    /// <summary>Creates a new 2D pose.</summary>
    /// <param name="x">Position along the X axis in meters.</param>
    /// <param name="y">Position along the Y axis in meters.</param>
    /// <param name="yaw">Heading in radians.</param>
    public Pose2D(double x, double y, double yaw)
    {
        X = x;
        Y = y;
        Yaw = yaw;
    }

    /// <summary>The origin pose at (0, 0) facing the positive X axis.</summary>
    public static readonly Pose2D Zero = new Pose2D(0, 0, 0);

    /// <inheritdoc />
    public bool Equals(Pose2D other)
    {
        return X.Equals(other.X)
            && Y.Equals(other.Y)
            && Yaw.Equals(other.Yaw);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Pose2D other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + X.GetHashCode();
            hash = hash * 31 + Y.GetHashCode();
            hash = hash * 31 + Yaw.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"Pose2D[X={X:F3}m, Y={Y:F3}m, Yaw={Yaw:F4}rad]";
}
