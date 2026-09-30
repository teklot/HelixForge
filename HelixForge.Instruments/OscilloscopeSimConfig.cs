using System;

namespace HelixForge.Instruments;

/// <summary>
/// Configuration for a simulated oscilloscope device.
/// </summary>
public sealed class OscilloscopeSimConfig
{
    /// <summary>Gets or sets the sample rate in samples per second. Default is 250000.</summary>
    public double SampleRate { get; set; } = 250000.0;

    /// <summary>Gets or sets the number of channels. Default is 4.</summary>
    public int ChannelCount { get; set; } = 4;

    /// <summary>Gets or sets the voltage range per channel in volts peak. Default is 5.0.</summary>
    public double VoltageRange { get; set; } = 5.0;

    /// <summary>Gets or sets the amplitude in volts of the primary signal. Default is 3.3.</summary>
    public double Amplitude { get; set; } = 3.3;

    /// <summary>Gets or sets the signal frequency in hertz. Default is 1000.0.</summary>
    public double FrequencyHz { get; set; } = 1000.0;

    /// <summary>Gets or sets the signal shape. Default is <see cref="OscilloscopeWaveformShape.Sine"/>.</summary>
    public OscilloscopeWaveformShape WaveformShape { get; set; } = OscilloscopeWaveformShape.Sine;

    /// <summary>Gets or sets the amount of additive noise. Default is 0.0.</summary>
    public double NoiseAmplitude { get; set; } = 0.0;

    /// <summary>Gets or sets the seed used to initialize the deterministic noise generator. Default is 0.</summary>
    public int RandomSeed { get; set; } = 0;
}
