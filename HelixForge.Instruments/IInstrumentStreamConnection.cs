using System;

namespace HelixForge.Instruments;

/// <summary>
/// Contract for a byte-stream connection to an instrument. Transport-agnostic:
/// instrument logic operates purely on bytes, so HelixForge never depends on a
/// concrete transport or on SignalFlux types.
/// </summary>
public interface IInstrumentStreamConnection : IDisposable
{
    /// <summary>True when the connection is open.</summary>
    bool IsOpen { get; }

    /// <summary>Writes bytes to the remote instrument endpoint.</summary>
    void Write(byte[] bytes);

    /// <summary>Reads any bytes currently available from the instrument.</summary>
    byte[] ReadAvailable();
}
