using System;
using System.IO.Ports;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// <see cref="ISerialTransport"/> implementation backed by <see cref="SerialPort"/>.
/// </summary>
public sealed class SerialPortTransport : ISerialTransport
{
    private const int ReadTimeoutMs = 100;

    private readonly SerialPort _port;

    /// <summary>Creates a serial transport for the given port and baud rate.</summary>
    /// <param name="portName">Serial port name (e.g., "COM3" or "/dev/ttyAMA0").</param>
    /// <param name="baudRate">Baud rate (typically 9600 or 115200).</param>
    public SerialPortTransport(string portName, int baudRate)
    {
        _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = ReadTimeoutMs
        };
    }

    /// <inheritdoc />
    public bool IsOpen => _port.IsOpen;

    /// <inheritdoc />
    public int BytesToRead => _port.BytesToRead;

    /// <inheritdoc />
    public void Open() => _port.Open();

    /// <inheritdoc />
    public void Close() => _port.Close();

    /// <inheritdoc />
    public byte[] ReadAvailable()
    {
        int available = _port.BytesToRead;
        if (available <= 0)
            return Array.Empty<byte>();

        var buffer = new byte[available];
        try
        {
            int read = _port.Read(buffer, 0, available);
            if (read == available)
                return buffer;
            if (read <= 0)
                return Array.Empty<byte>();

            var trimmed = new byte[read];
            Array.Copy(buffer, trimmed, read);
            return trimmed;
        }
        catch (TimeoutException)
        {
            return Array.Empty<byte>();
        }
    }

    /// <inheritdoc />
    public void Write(byte[] bytes)
    {
        _port.Write(bytes, 0, bytes.Length);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_port.IsOpen)
            _port.Close();
        _port.Dispose();
    }
}
