using System;

namespace HelixForge.Instruments;

/// <summary>
/// Contract for an oscilloscope instrument. Allows a single capture of
/// channel data to be requested and read from the device.
/// </summary>
public interface IOscilloscopeDevice : IDisposable
{
    /// <summary>Unique identifier for the device.</summary>
    string DeviceId { get; }

    /// <summary>True once the device has been initialized.</summary>
    bool IsInitialized { get; }

    /// <summary>Number of configured channels.</summary>
    int ChannelCount { get; }

    /// <summary>Sample rate in samples per second.</summary>
    double SampleRate { get; }

    /// <summary>Initializes the device.</summary>
    void Initialize();

    /// <summary>Arms a capture. Repeated calls re-arm and reset the capture state.</summary>
    void Arm();

    /// <summary>
    /// Reads the latest full capture. If no capture has been armed and
    /// completed, the device performs a single capture on demand.
    /// </summary>
    OscilloscopeData ReadCapture();
}
