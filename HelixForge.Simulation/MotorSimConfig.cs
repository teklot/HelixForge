namespace HelixForge.Simulation;

/// <summary>
/// Configuration for simulated motor behavior.
/// </summary>
public sealed class MotorSimConfig
{
    /// <summary>Maximum RPM at full throttle. Default is 10000.</summary>
    public double MaxRpm { get; set; } = 10000;

    /// <summary>Response time constant in seconds (time to reach 63% of target). Default is 0.05.</summary>
    public double ResponseTimeConstant { get; set; } = 0.05;

    /// <summary>Minimum throttle threshold to start spinning. Default is 0.05.</summary>
    public double Deadband { get; set; } = 0.05;

    /// <summary>Initial throttle value. Default is 0.0.</summary>
    public double InitialThrottle { get; set; } = 0.0;

    /// <summary>Current drawn at full throttle and full RPM, in amperes. Default is 0.0 (no draw).</summary>
    public double MaxCurrentAmps { get; set; } = 0.0;
}
