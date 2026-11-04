using System;
using System.Device.Spi;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// Minimal SPI bus abstraction used by hardware drivers. Implemented for real
/// hardware by <see cref="SpiBusAdapter"/> and by fakes in unit tests.
/// </summary>
public interface ISpiBus : IDisposable
{
    /// <summary>Opens a device on the given chip-select line.</summary>
    /// <param name="chipSelectLine">Chip-select line number.</param>
    /// <param name="clockFrequencyHz">SPI clock frequency in hertz.</param>
    /// <param name="mode">SPI clock mode.</param>
    /// <returns>The opened device. Its lifetime is bound to this bus.</returns>
    ISpiDevice OpenDevice(int chipSelectLine, int clockFrequencyHz, SpiMode mode);
}
