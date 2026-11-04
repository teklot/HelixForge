using System;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// Minimal SPI device abstraction: read, write, and full-duplex transfers.
/// </summary>
public interface ISpiDevice : IDisposable
{
    /// <summary>Reads bytes from the device (clocking out zeros).</summary>
    /// <param name="buffer">Destination buffer.</param>
    void Read(Span<byte> buffer);

    /// <summary>Writes bytes to the device.</summary>
    /// <param name="buffer">Bytes to write.</param>
    void Write(ReadOnlySpan<byte> buffer);

    /// <summary>Performs a full-duplex transfer.</summary>
    /// <param name="writeBuffer">Bytes clocked out to the device.</param>
    /// <param name="readBuffer">Bytes clocked in from the device; same length as <paramref name="writeBuffer"/>.</param>
    void TransferFullDuplex(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer);
}
