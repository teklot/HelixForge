using System;

namespace HelixForge.Instruments;

/// <summary>
/// Contract for a data acquisition device. Reports a set of measured
/// channels on request, in a deterministic, clock-driven fashion.
/// </summary>
public interface IDaqDevice : IDisposable
{
    /// <summary>Unique identifier for the device.</summary>
    string DeviceId { get; }

    /// <summary>True once the device has been initialized.</summary>
    bool IsInitialized { get; }

    /// <summary>Number of configured analog input channels.</summary>
    int ChannelCount { get; }

    /// <summary>Initializes the device.</summary>
    void Initialize();

    /// <summary>Reads the latest scan from every configured channel.</summary>
    DaqScan ReadScan();
}