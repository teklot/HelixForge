using System;

namespace HelixForge.Instruments;

/// <summary>
/// Configuration for a simulated battery cycler.
/// </summary>
public sealed class BatteryCyclerSimConfig
{
    /// <summary>Gets or sets the cell capacity in ampere-hours. Default is 3.0.</summary>
    public double CapacityAh { get; set; } = 3.0;

    /// <summary>Gets or sets the cell nominal voltage in volts. Default is 3.7.</summary>
    public double NominalVoltage { get; set; } = 3.7;

    /// <summary>Gets or sets the time step per clock tick in seconds. Default is 1.0.</summary>
    public double TimeStepSeconds { get; set; } = 1.0;

    /// <summary>Gets or sets the initial charge fraction. Default is 0.5.</summary>
    public double InitialChargeFraction { get; set; } = 0.5;
}