using System;

namespace HelixForge;

/// <summary>
/// Snapshot of IMU sensor data at a specific point in time.
/// All rotations use radians.
/// </summary>
public readonly struct ImuData : IEquatable<ImuData>
{
    /// <summary>Current orientation as Euler angles (roll, pitch, yaw) in radians.</summary>
    public Vector3 Orientation { get; }

    /// <summary>Angular velocity in radians per second.</summary>
    public Vector3 AngularVelocity { get; }

    /// <summary>Linear acceleration in meters per second squared.</summary>
    public Vector3 Acceleration { get; }

    /// <summary>Time of this reading.</summary>
    public TimeSpan Timestamp { get; }

    /// <summary>Creates a new IMU data snapshot.</summary>
    /// <param name="orientation">Euler angles (roll, pitch, yaw) in radians.</param>
    /// <param name="angularVelocity">Angular velocity in radians per second.</param>
    /// <param name="acceleration">Linear acceleration in meters per second squared.</param>
    /// <param name="timestamp">Time of this reading.</param>
    public ImuData(Vector3 orientation, Vector3 angularVelocity, Vector3 acceleration, TimeSpan timestamp)
    {
        Orientation = orientation;
        AngularVelocity = angularVelocity;
        Acceleration = acceleration;
        Timestamp = timestamp;
    }

    /// <summary>An empty IMU reading with zero values.</summary>
    public static readonly ImuData Empty = new ImuData(Vector3.Zero, Vector3.Zero, Vector3.Zero, TimeSpan.Zero);

    /// <inheritdoc />
    public bool Equals(ImuData other)
    {
        return Orientation.Equals(other.Orientation)
            && AngularVelocity.Equals(other.AngularVelocity)
            && Acceleration.Equals(other.Acceleration)
            && Timestamp.Equals(other.Timestamp);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ImuData other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Orientation.GetHashCode();
            hash = hash * 31 + AngularVelocity.GetHashCode();
            hash = hash * 31 + Acceleration.GetHashCode();
            hash = hash * 31 + Timestamp.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"ImuData[Orient={Orientation}, AngVel={AngularVelocity}, Accel={Acceleration}, T={Timestamp.TotalMilliseconds:F1}ms]";
}
