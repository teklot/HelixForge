using System;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// Minimal byte-stream serial transport used by GPS drivers. Implemented for
/// real hardware by <see cref="SerialPortTransport"/> and by fakes in unit tests.
/// </summary>
public interface ISerialTransport : IDisposable
{
    /// <summary>Gets whether the transport is open.</summary>
    bool IsOpen { get; }

    /// <summary>Gets the number of bytes currently available to read.</summary>
    int BytesToRead { get; }

    /// <summary>Opens the transport.</summary>
    void Open();

    /// <summary>Closes the transport without disposing it.</summary>
    void Close();

    /// <summary>Reads all bytes currently available.</summary>
    /// <returns>The available bytes, or an empty array when none are available.</returns>
    byte[] ReadAvailable();

    /// <summary>Writes bytes to the transport.</summary>
    /// <param name="bytes">Bytes to write.</param>
    void Write(byte[] bytes);
}
