using System;

namespace HelixForge.Instruments;

/// <summary>
/// Contract for a programmable logic controller (PLC). Provides a set of
/// discrete inputs and outputs with a cyclic scan model.
/// </summary>
public interface IPlcDevice : IDisposable
{
    /// <summary>Unique identifier for the device.</summary>
    string DeviceId { get; }

    /// <summary>True once the device has been initialized.</summary>
    bool IsInitialized { get; }

    /// <summary>Number of digital input points (I0..In).</summary>
    int InputCount { get; }

    /// <summary>Number of digital output points (Q0..Qn).</summary>
    int OutputCount { get; }

    /// <summary>Initializes the device.</summary>
    void Initialize();

    /// <summary>Sets the state of a digital input point.</summary>
    void SetInput(int index, bool state);

    /// <summary>Reads the state of a digital input point.</summary>
    bool ReadInput(int index);

    /// <summary>Reads the state of a digital output point.</summary>
    bool ReadOutput(int index);

    /// <summary>Advances the PLC by one scan cycle, evaluating the ladder logic.</summary>
    void ScanOnce();
}