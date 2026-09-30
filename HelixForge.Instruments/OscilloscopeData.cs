using System;

namespace HelixForge.Instruments;

/// <summary>
/// A full capture of channel data from an oscilloscope device.
/// </summary>
public sealed class OscilloscopeData
{
    /// <summary>
    /// Creates a new oscilloscope capture.
    /// </summary>
    /// <param name="deviceId">The identifier of the producing device.</param>
    /// <param name="channels">Captured channel data.</param>
    /// <param name="sampleRate">Sample rate of the capture in samples per second.</param>
    /// <param name="capturedAt">Simulation time at which the capture was obtained.</param>
    public OscilloscopeData(string deviceId, OscilloscopeChannelData[] channels, double sampleRate, TimeSpan capturedAt)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        Channels = channels ?? throw new ArgumentNullException(nameof(channels));
        SampleRate = sampleRate;
        CapturedAt = capturedAt;
    }

    /// <summary>Gets the identifier of the producing device.</summary>
    public string DeviceId { get; }

    /// <summary>Gets the captured channel data.</summary>
    public OscilloscopeChannelData[] Channels { get; }

    /// <summary>Gets the sample rate in samples per second.</summary>
    public double SampleRate { get; }

    /// <summary>Gets the simulation time at which the capture was obtained.</summary>
    public TimeSpan CapturedAt { get; }

    /// <summary>Gets the number of captured channels.</summary>
    public int ChannelCount => Channels.Length;
}
