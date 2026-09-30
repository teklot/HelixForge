using System;

namespace HelixForge.Instruments;

/// <summary>
/// A single channel of captured oscilloscope sample data.
/// </summary>
public sealed class OscilloscopeChannelData
{
    /// <summary>Creates a new oscilloscope channel data record.</summary>
    /// <param name="channelName">Name of the channel (e.g., "CH1").</param>
    /// <param name="samples">Captured voltage samples in volts.</param>
    public OscilloscopeChannelData(string channelName, double[] samples)
    {
        ChannelName = channelName ?? throw new ArgumentNullException(nameof(channelName));
        Samples = samples ?? throw new ArgumentNullException(nameof(samples));
    }

    /// <summary>Gets the channel name.</summary>
    public string ChannelName { get; }

    /// <summary>Gets the captured voltage samples in volts.</summary>
    public double[] Samples { get; }

    /// <summary>Gets the number of captured samples.</summary>
    public int SampleCount => Samples.Length;
}
