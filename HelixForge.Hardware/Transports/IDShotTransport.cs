using System;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// Seam for transmitting encoded DShot frames. Real DShot requires microsecond
/// timing that managed code cannot guarantee, so production implementations are
/// expected to hand frames to a co-processor (e.g. a flight-controller MCU).
/// </summary>
public interface IDShotTransport : IDisposable
{
    /// <summary>Sends a complete 16-bit DShot frame.</summary>
    /// <param name="frame">Encoded frame produced by <see cref="Drivers.DShotCodec"/>.</param>
    void SendFrame(ushort frame);
}
