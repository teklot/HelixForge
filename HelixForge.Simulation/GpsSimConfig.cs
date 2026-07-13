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
}
