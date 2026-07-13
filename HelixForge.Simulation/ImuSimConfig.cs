namespace HelixForge.Simulation;

/// <summary>
/// Configuration for simulated IMU behavior.
/// </summary>
public sealed class ImuSimConfig
{
    /// <summary>Standard deviation of accelerometer noise (m/s^2). Default is 0.01.</summary>
    public double AccelerometerNoise { get; set; } = 0.01;

    /// <summary>Standard deviation of gyroscope noise (rad/s). Default is 0.005.</summary>
    public double GyroscopeNoise { get; set; } = 0.005;

    /// <summary>Accelerometer drift rate per second. Default is 0.001.</summary>
    public double AccelerometerDrift { get; set; } = 0.001;

    /// <summary>Gyroscope drift rate per second. Default is 0.0005.</summary>
    public double GyroscopeDrift { get; set; } = 0.0005;

    /// <summary>Initial orientation in radians (roll, pitch, yaw). Default is zero.</summary>
    public Vector3 InitialOrientation { get; set; } = Vector3.Zero;

    /// <summary>Random seed for deterministic noise generation. Null uses system clock.</summary>
    public int? RandomSeed { get; set; }
}
