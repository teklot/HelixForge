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

    /// <summary>Bias-instability random-walk rate per axis per second. Default is 0.0 (disabled).</summary>
    public double BiasInstability { get; set; } = 0.0;

    /// <summary>Gyro bias added per degree of temperature deviation from the operating point (rad/s per °C). Default is 0.0.</summary>
    public double TemperatureCoefficient { get; set; } = 0.0;

    /// <summary>Operating temperature in degrees Celsius where the temperature coefficient applies. Default is 25.0.</summary>
    public double OperatingTemperature { get; set; } = 25.0;

    /// <summary>Current simulated temperature in degrees Celsius. Default is 25.0.</summary>
    public double Temperature { get; set; } = 25.0;
}
