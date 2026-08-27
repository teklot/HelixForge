namespace HelixForge.Simulation;

/// <summary>
/// Configuration for simulated servo behavior.
/// </summary>
public sealed class ServoSimConfig
{
    /// <summary>Minimum achievable angle in degrees. Default is 0.0.</summary>
    public double MinAngle { get; set; } = 0.0;

    /// <summary>Maximum achievable angle in degrees. Default is 180.0.</summary>
    public double MaxAngle { get; set; } = 180.0;

    /// <summary>Maximum travel speed in degrees per second. Default is 60.0.</summary>
    public double SlewRate { get; set; } = 60.0;

    /// <summary>Initial angle in degrees. Default is 0.0.</summary>
    public double InitialAngle { get; set; } = 0.0;
}
