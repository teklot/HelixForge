namespace HelixForge.Simulation;

/// <summary>
/// Configuration for simulated GPS behavior.
/// </summary>
public sealed class GpsSimConfig
{
    /// <summary>Standard deviation of position noise in meters. Default is 2.0.</summary>
    public double PositionNoise { get; set; } = 2.0;

    /// <summary>Initial latitude in decimal degrees. Default is 0.0.</summary>
    public double InitialLatitude { get; set; } = 0.0;

    /// <summary>Initial longitude in decimal degrees. Default is 0.0.</summary>
    public double InitialLongitude { get; set; } = 0.0;

    /// <summary>Initial altitude in meters. Default is 0.0.</summary>
    public double InitialAltitude { get; set; } = 0.0;

    /// <summary>Random seed for deterministic noise generation. Null uses system clock.</summary>
    public int? RandomSeed { get; set; }

    /// <summary>Base fix status when not in a dropout window. Default is Fix3D.</summary>
    public GpsFixStatus BaseFixStatus { get; set; } = GpsFixStatus.Fix3D;

    /// <summary>Probability per second of entering a dropout window. Default is 0.0 (no dropouts).</summary>
    public double DropoutRate { get; set; } = 0.0;

    /// <summary>Maximum duration of a single dropout window in seconds. Default is 1.0.</summary>
    public double MaxDropoutDuration { get; set; } = 1.0;

    /// <summary>Position noise applied during a dropout window, in meters. Default is 50.0.</summary>
    public double UncertaintyDuringDropout { get; set; } = 50.0;

    /// <summary>Optional device-scoped environment model adding wind to velocity and reference altitude. Default is null.</summary>
    public EnvironmentSimConfig? Environment { get; set; }
}
