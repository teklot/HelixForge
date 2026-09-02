using System;

namespace HelixForge;

/// <summary>
/// Snapshot of differential-drive odometry at a specific point in time.
/// </summary>
public readonly struct DifferentialDriveData : IEquatable<DifferentialDriveData>
{
    /// <summary>Estimated vehicle pose.</summary>
    public Pose2D Pose { get; }

    /// <summary>Linear speed in meters per second.</summary>
    public double LinearSpeed { get; }

    /// <summary>Angular velocity in radians per second.</summary>
    public double AngularSpeed { get; }

    /// <summary>Time of this reading.</summary>
    public TimeSpan Timestamp { get; }

    /// <summary>Creates a new differential-drive odometry snapshot.</summary>
    /// <param name="pose">Estimated vehicle pose.</param>
    /// <param name="linearSpeed">Linear speed in meters per second.</param>
    /// <param name="angularSpeed">Angular velocity in radians per second.</param>
    /// <param name="timestamp">Time of this reading.</param>
    public DifferentialDriveData(Pose2D pose, double linearSpeed, double angularSpeed, TimeSpan timestamp)
    {
        Pose = pose;
        LinearSpeed = linearSpeed;
        AngularSpeed = angularSpeed;
        Timestamp = timestamp;
    }

    /// <summary>An empty differential-drive reading with zero values.</summary>
    public static readonly DifferentialDriveData Empty = new DifferentialDriveData(Pose2D.Zero, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public bool Equals(DifferentialDriveData other)
    {
        return Pose.Equals(other.Pose)
            && LinearSpeed.Equals(other.LinearSpeed)
            && AngularSpeed.Equals(other.AngularSpeed)
            && Timestamp.Equals(other.Timestamp);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DifferentialDriveData other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Pose.GetHashCode();
            hash = hash * 31 + LinearSpeed.GetHashCode();
            hash = hash * 31 + AngularSpeed.GetHashCode();
            hash = hash * 31 + Timestamp.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"DifferentialDriveData[Pose=({Pose.X:F2},{Pose.Y:F2}) {Pose.Yaw:F3}rad, V={LinearSpeed:F2}m/s, W={AngularSpeed:F3}rad/s]";
}
