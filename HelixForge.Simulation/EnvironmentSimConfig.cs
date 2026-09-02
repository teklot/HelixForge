namespace HelixForge.Simulation;

/// <summary>
/// Device-scoped environment model providing wind, turbulence, and a reference
/// altitude. Passed as a per-device copy so no shared global state exists
/// (PRD §12: simulation stays strictly device-local).
/// </summary>
public sealed class EnvironmentSimConfig
{
    /// <summary>Steady wind velocity in meters per second (X, Y, Z world frame). Default is zero.</summary>
    public Vector3 WindVelocity { get; set; } = Vector3.Zero;

    /// <summary>Standard deviation of wind gust noise in meters per second. Default is 0.0.</summary>
    public double WindGustNoise { get; set; } = 0.0;

    /// <summary>Turbulence magnitude as a standard deviation applied to sensor noise. Default is 0.0.</summary>
    public double Turbulence { get; set; } = 0.0;

    /// <summary>Reference altitude the environment represents, in meters. Default is 0.0.</summary>
    public double ReferenceAltitudeMeters { get; set; } = 0.0;

    /// <summary>Random seed for deterministic noise generation. Null uses system clock.</summary>
    public int? RandomSeed { get; set; }
}
