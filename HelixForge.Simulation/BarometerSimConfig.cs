namespace HelixForge.Simulation;

/// <summary>
/// Configuration for simulated barometer behavior.
/// </summary>
public sealed class BarometerSimConfig
{
    /// <summary>Sea-level pressure in hPa used for altitude derivation. Default is 1013.25.</summary>
    public double SeaLevelPressure { get; set; } = 1013.25;

    /// <summary>Standard deviation of altitude noise in meters. Default is 0.5.</summary>
    public double AltitudeNoise { get; set; } = 0.5;

    /// <summary>Barometric drift rate per second in hPa. Default is 0.0.</summary>
    public double DriftRate { get; set; } = 0.0;

    /// <summary>Initial altitude in meters. Default is 0.0.</summary>
    public double InitialAltitude { get; set; } = 0.0;

    /// <summary>Random seed for deterministic noise generation. Null uses system clock.</summary>
    public int? RandomSeed { get; set; }
}
