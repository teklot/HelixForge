using System;
using System.Buffers.Binary;
using System.Diagnostics;
using HelixForge.Hardware.Transports;

namespace HelixForge.Hardware.Drivers;

/// <summary>
/// Real GPS device driver that parses u-blox UBX NAV-PVT frames over a serial
/// transport. Frames are validated with the UBX Fletcher-8 checksum; invalid or
/// unrelated frames are discarded.
/// </summary>
public sealed class UbxGpsDevice : IGpsDevice
{
    private const byte Sync1 = 0xB5;
    private const byte Sync2 = 0x62;
    private const byte NavPvtClass = 0x01;
    private const byte NavPvtId = 0x07;
    private const int NavPvtPayloadLength = 92;
    private const int MaxPayloadLength = 1024;
    private const int BufferCapacity = 4096;

    private readonly string _portName = string.Empty;
    private readonly int _baudRate;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private readonly byte[] _buffer = new byte[BufferCapacity];
    private ISerialTransport? _transport;
    private int _bufferCount;
    private GpsData _latestReading;

    /// <inheritdoc />
    public string DeviceId { get; }

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public GpsData LatestReading => _latestReading;

    /// <summary>Creates a new UBX GPS device on a physical serial port.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="portName">Serial port name (e.g., "COM3" or "/dev/ttyAMA0").</param>
    /// <param name="baudRate">Baud rate (typically 115200 for UBX).</param>
    public UbxGpsDevice(string deviceId, string portName, int baudRate = 115200)
    {
        DeviceId = deviceId;
        _portName = portName;
        _baudRate = baudRate;
        _latestReading = GpsData.Empty;
    }

    /// <summary>Creates a new UBX GPS device on a supplied serial transport.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="transport">Serial transport. The driver takes ownership of it.</param>
    public UbxGpsDevice(string deviceId, ISerialTransport transport)
    {
        DeviceId = deviceId;
        _transport = transport;
        _latestReading = GpsData.Empty;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        ISerialTransport transport = _transport ?? (_transport = new SerialPortTransport(_portName, _baudRate));
        if (!transport.IsOpen)
            transport.Open();

        _bufferCount = 0;
        _stopwatch.Restart();
        IsInitialized = true;
    }

    /// <inheritdoc />
    public GpsData Read()
    {
        if (!IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is not initialized.");

        if (_transport != null && _transport.BytesToRead > 0)
        {
            Append(_transport.ReadAvailable());
            ProcessBuffer();
        }

        return _latestReading;
    }

    /// <inheritdoc />
    public void Update(TimeSpan deltaTime)
    {
        Read();
    }

    /// <inheritdoc />
    public void Reset()
    {
        Dispose();
        _latestReading = GpsData.Empty;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopwatch.Reset();
        _transport?.Dispose();
        _transport = null;
        _bufferCount = 0;
        IsInitialized = false;
    }

    private void Append(byte[] bytes)
    {
        if (bytes.Length == 0)
            return;

        if (bytes.Length >= _buffer.Length)
        {
            Buffer.BlockCopy(bytes, bytes.Length - _buffer.Length, _buffer, 0, _buffer.Length);
            _bufferCount = _buffer.Length;
            return;
        }

        if (_bufferCount + bytes.Length > _buffer.Length)
        {
            int overflow = _bufferCount + bytes.Length - _buffer.Length;
            Buffer.BlockCopy(_buffer, overflow, _buffer, 0, _bufferCount - overflow);
            _bufferCount -= overflow;
        }

        Buffer.BlockCopy(bytes, 0, _buffer, _bufferCount, bytes.Length);
        _bufferCount += bytes.Length;
    }

    private void ProcessBuffer()
    {
        int index = 0;

        while (true)
        {
            if (_bufferCount - index < 8)
                break;

            if (_buffer[index] != Sync1 || _buffer[index + 1] != Sync2)
            {
                index++;
                continue;
            }

            int payloadLength = _buffer[index + 4] | (_buffer[index + 5] << 8);
            if (payloadLength > MaxPayloadLength)
            {
                index++;
                continue;
            }

            int frameLength = payloadLength + 8;
            if (_bufferCount - index < frameLength)
                break;

            ComputeChecksum(_buffer.AsSpan(index + 2, payloadLength + 4), out byte expectedA, out byte expectedB);
            byte actualA = _buffer[index + frameLength - 2];
            byte actualB = _buffer[index + frameLength - 1];

            if (actualA != expectedA || actualB != expectedB)
            {
                index++;
                continue;
            }

            if (_buffer[index + 2] == NavPvtClass
                && _buffer[index + 3] == NavPvtId
                && payloadLength == NavPvtPayloadLength)
            {
                ParseNavPvt(_buffer.AsSpan(index + 6, payloadLength));
            }

            index += frameLength;
        }

        RemoveConsumed(index);
    }

    private void RemoveConsumed(int count)
    {
        if (count <= 0)
            return;

        if (count >= _bufferCount)
        {
            _bufferCount = 0;
            return;
        }

        Buffer.BlockCopy(_buffer, count, _buffer, 0, _bufferCount - count);
        _bufferCount -= count;
    }

    private void ParseNavPvt(ReadOnlySpan<byte> payload)
    {
        int longitude = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(24, 4));
        int latitude = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(28, 4));
        int heightMsl = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(36, 4));
        int velocityNorth = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(48, 4));
        int velocityEast = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(52, 4));
        int velocityDown = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(56, 4));
        byte fixType = payload[20];

        var velocity = new Vector3(
            velocityNorth / 1000.0,
            velocityEast / 1000.0,
            -velocityDown / 1000.0);

        _latestReading = new GpsData(
            latitude * 1e-7,
            longitude * 1e-7,
            heightMsl / 1000.0,
            velocity,
            _stopwatch.Elapsed,
            MapFixStatus(fixType));
    }

    private static GpsFixStatus MapFixStatus(byte fixType)
    {
        return fixType switch
        {
            2 => GpsFixStatus.Fix2D,
            3 or 4 => GpsFixStatus.Fix3D,
            _ => GpsFixStatus.NoFix
        };
    }

    private static void ComputeChecksum(ReadOnlySpan<byte> data, out byte checksumA, out byte checksumB)
    {
        byte a = 0;
        byte b = 0;

        foreach (byte value in data)
        {
            a += value;
            b += a;
        }

        checksumA = a;
        checksumB = b;
    }
}
