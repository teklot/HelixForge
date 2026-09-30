using System;

namespace HelixForge.Instruments;

/// <summary>
/// Configuration for a simulated bench power supply.
/// </summary>
public sealed class BenchPsuSimConfig
{
    /// <summary>Gets or sets the maximum output voltage in volts. Default is 30.0.</summary>
    public double MaxVoltage { get; set; } = 30.0;

    /// <summary>Gets or sets the maximum output current in amperes. Default is 5.0.</summary>
    public double MaxCurrent { get; set; } = 5.0;

    /// <summary>Gets or sets the default output voltage in volts. Default is 0.0.</summary>
    public double DefaultVoltage { get; set; } = 0.0;

    /// <summary>Gets or sets the load resistance in ohms reflected on the output. Default is 100.0.</summary>
    public double LoadResistance { get; set; } = 100.0;

    /// <summary>Gets or sets the voltage ramp rate in volts per clock tick. Default is 0.5.</summary>
    public double VoltageStepPerClock { get; set; } = 0.5;
}