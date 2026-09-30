using System;

namespace HelixForge.Instruments;

/// <summary>
/// A single scan of DAQ channels, capturing the values at one timestamp.
/// </summary>
public sealed class DaqScan
{
    /// <summary>
    /// Creates a new DAQ scan.
    /// </summary>
    /// <param name="deviceId">Identifier of the producing device.</param>
    /// <param name="channels">Channel values in this scan.</param>
    /// <param name="sampledAt">Simulation time of the scan.</param>
    public DaqScan(string deviceId, DaqChannelData[] channels, TimeSpan sampledAt)
    {
        DeviceId = deviceId;
        Channels = channels;
        SampledAt = sampledAt;
    }

    /// <summary>Gets the identifier of the producing device.</summary>
    public string DeviceId { get; }

    /// <summary>Gets the channel values in this scan.</summary>
    public DaqChannelData[] Channels { get; }

    /// <summary>Gets the simulation time of this scan.</summary>
    public TimeSpan SampledAt { get; }
}