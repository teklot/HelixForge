using System;

namespace HelixForge.Simulation;

/// <summary>
/// Configuration for simulated magnetometer behavior.
/// </summary>
public sealed class MagSimConfig
{
    /// <summary>Earth's magnetic field vector in microteslas (body frame reference). Default points north-level.</summary>
    public Vector3 EarthField { get; set; } = new Vector3(25.0, 0.0, -45.0);

    /// <summary>Fixed hard-iron offset in microteslas added to the field. Default is zero.</summary>
    public Vector3 HardIronOffset { get; set; } = Vector3.Zero;

    /// <summary>Soft-iron scale applied per-axis to the field. Default is identity.</summary>
    public Vector3 SoftIronScale { get; set; } = new Vector3(1.0, 1.0, 1.0);

    /// <summary>Magnetic declination in degrees added to the horizontal field. Default is 0.0.</summary>
    public double DeclinationDeg { get; set; } = 0.0;

    /// <summary>Standard deviation of reading noise in microteslas. Default is 0.1.</summary>
    public double MagneticNoise { get; set; } = 0.1;

    /// <summary>Random seed for deterministic noise generation. Null uses system clock.</summary>
    public int? RandomSeed { get; set; }
}
