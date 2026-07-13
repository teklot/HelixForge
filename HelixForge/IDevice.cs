using System;

namespace HelixForge;

/// <summary>
/// Base interface for all hardware devices in the abstraction layer.
/// Provides lifecycle management independent of execution mode.
/// </summary>
public interface IDevice : IDisposable
{
    /// <summary>
    /// Unique identifier for this device instance (e.g., "imu-01", "motor-left").
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// Gets whether this device has been initialized and is ready for use.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the device for operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the device is already initialized.</exception>
    void Initialize();

    /// <summary>
    /// Resets the device to its initial state without full disposal.
    /// </summary>
    void Reset();
}
