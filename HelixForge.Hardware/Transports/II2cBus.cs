using System;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// Minimal I2C bus abstraction used by hardware drivers. Implemented for real
/// hardware by <see cref="I2cBusAdapter"/> and by fakes in unit tests.
/// </summary>
public interface II2cBus : IDisposable
{
    /// <summary>Opens the device at the specified 7-bit I2C address.</summary>
    /// <param name="deviceAddress">7-bit I2C device address.</param>
    /// <returns>The opened device. Its lifetime is bound to this bus.</returns>
    II2cDevice OpenDevice(int deviceAddress);
}
