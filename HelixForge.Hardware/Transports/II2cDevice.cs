using System;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// Minimal I2C device abstraction: single-shot and combined write/read transfers.
/// </summary>
public interface II2cDevice : IDisposable
{
    /// <summary>Gets the 7-bit I2C address of this device.</summary>
    int DeviceAddress { get; }

    /// <summary>Reads bytes from the device.</summary>
    /// <param name="buffer">Destination buffer.</param>
    void Read(Span<byte> buffer);

    /// <summary>Writes bytes to the device.</summary>
    /// <param name="buffer">Bytes to write.</param>
    void Write(ReadOnlySpan<byte> buffer);

    /// <summary>
    /// Performs an atomic write followed by a read (repeated-start transfer),
    /// used for register reads.
    /// </summary>
    /// <param name="writeBuffer">Bytes written before the read.</param>
    /// <param name="readBuffer">Destination buffer for the read phase.</param>
    void WriteRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer);
}
