namespace HelixForge.Simulation;

/// <summary>
/// Configuration for simulated battery behavior.
/// </summary>
public sealed class BatterySimConfig
{
    /// <summary>Fully-charged open-circuit voltage in volts. Default is 12.6.</summary>
    public double FullVoltage { get; set; } = 12.6;

    /// <summary>Empty open-circuit voltage in volts. Default is 9.0.</summary>
    public double EmptyVoltage { get; set; } = 9.0;

    /// <summary>Internal series resistance in ohms, producing sag under load. Default is 0.05.</summary>
    public double InternalResistance { get; set; } = 0.05;

    /// <summary>Total capacity in ampere-hours. Default is 5.0.</summary>
    public double CapacityAh { get; set; } = 5.0;

    /// <summary>Initial state of charge as a fraction in [0.0, 1.0]. Default is 1.0.</summary>
    public double InitialCharge { get; set; } = 1.0;

    /// <summary>Random seed for deterministic noise generation. Null uses system clock.</summary>
    public int? RandomSeed { get; set; }
}
