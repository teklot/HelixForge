using System;
using System.IO;

namespace HelixForge.Instruments;

/// <summary>
/// In-memory byte-stream connection used by simulations and tests. Bytes written
/// to one endpoint are read from the peer via <see cref="ReadAvailable"/>.
/// </summary>
public sealed class InMemoryInstrumentConnection : IInstrumentStreamConnection
{
    private readonly MemoryStream _incoming = new MemoryStream();
    private bool _disposed;

    /// <inheritdoc/>
    public bool IsOpen => !_disposed;

    /// <summary>Writes bytes into this endpoint's incoming buffer.</summary>
    public void Feed(byte[] bytes)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(InMemoryInstrumentConnection));
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        _incoming.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc/>
    public void Write(byte[] bytes)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(InMemoryInstrumentConnection));
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        // Peer reads these from the same shared buffer; kept symmetrical so a
        // responder writing a reply can have it read back from ReadAvailable.
        _incoming.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc/>
    public byte[] ReadAvailable()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(InMemoryInstrumentConnection));
        byte[] data = _incoming.ToArray();
        _incoming.SetLength(0);
        _incoming.Position = 0;
        return data;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _incoming.Dispose();
    }
}