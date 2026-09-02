namespace HelixForge.Simulation;

/// <summary>
/// Configuration for simulated differential-drive behavior.
/// </summary>
public sealed class DifferentialDriveSimConfig
{
    /// <summary>Distance between the left and right drive wheels in meters. Default is 0.3.</summary>
    public double TrackWidth { get; set; } = 0.3;

    /// <summary>Maximum achievable wheel speed in meters per second. Default is 1.0.</summary>
    public double MaxSpeed { get; set; } = 1.0;

    /// <summary>Initial pose of the vehicle. Default is the origin.</summary>
    public Pose2D InitialPose { get; set; } = Pose2D.Zero;

    /// <summary>Random seed for deterministic odometry noise generation. Null uses system clock.</summary>
    public int? RandomSeed { get; set; }

    /// <summary>Standard deviation of odometry noise in meters per step. Default is 0.0 (no noise).</summary>
    public double OdometryNoise { get; set; } = 0.0;
}
