using System;

namespace HelixForge.Instruments;

/// <summary>
/// Configuration for a simulated DAQ device.
/// </summary>
public sealed class DaqSimConfig
{
    /// <summary>Gets or sets the number of analog input channels. Default is 8.</summary>
    public int ChannelCount { get; set; } = 8;

    /// <summary>Gets or sets the analog input range in volts (symmetric around zero). Default is 10.0.</summary>
    public double VoltageRange { get; set; } = 10.0;

    /// <summary>Gets or sets the sample rate in samples per second. Default is 1000.0.</summary>
    public double SampleRate { get; set; } = 1000.0;

    /// <summary>Gets or sets the baseline voltage of every channel in volts. Default is 0.0.</summary>
    public double BaselineVoltage { get; set; } = 0.0;

    /// <summary>Gets or sets the per-channel signal amplitude in volts. Default is 1.0.</summary>
    public double Amplitude { get; set; } = 1.0;

    /// <summary>Gets or sets the amount of additive noise. Default is 0.0.</summary>
    public double NoiseAmplitude { get; set; } = 0.0;

    /// <summary>Gets or sets the seed for the deterministic noise generator. Default is 0.</summary>
    public int RandomSeed { get; set; } = 0;
}