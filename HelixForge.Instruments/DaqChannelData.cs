using System;

namespace HelixForge.Instruments;

/// <summary>
/// A channel of data acquired from a DAQ (data acquisition) device.
/// </summary>
public sealed class DaqChannelData
{
    /// <summary>
    /// Creates a new DAQ channel sample.
    /// </summary>
    /// <param name="channelName">Channel name, e.g. "AI0".</param>
    /// <param name="voltage">The measured voltage in volts.</param>
    /// <param name="sampledAt">Simulation time at which the sample was acquired.</param>
    public DaqChannelData(string channelName, double voltage, TimeSpan sampledAt)
    {
        ChannelName = channelName ?? throw new ArgumentNullException(nameof(channelName));
        Voltage = voltage;
        SampledAt = sampledAt;
    }

    /// <summary>Gets the channel name.</summary>
    public string ChannelName { get; }

    /// <summary>Gets the measured voltage in volts.</summary>
    public double Voltage { get; }

    /// <summary>Gets the simulation time at which the sample was acquired.</summary>
    public TimeSpan SampledAt { get; }
}