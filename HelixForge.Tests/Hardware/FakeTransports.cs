using System;
using System.Collections.Generic;
using System.Device.Spi;
using HelixForge.Hardware.Transports;

namespace HelixForge.Tests.Hardware;

public sealed class FakeSpiBus : ISpiBus
{
    private readonly byte _chipId;

    public FakeSpiBus(byte chipId = 0xD1)
    {
        _chipId = chipId;
    }

    public FakeSpiDevice? Device { get; private set; }

    public int ChipSelectLine { get; private set; }

    public int ClockFrequencyHz { get; private set; }

    public SpiMode Mode { get; private set; }

    public bool Disposed { get; private set; }

    public ISpiDevice OpenDevice(int chipSelectLine, int clockFrequencyHz, SpiMode mode)
    {
        ChipSelectLine = chipSelectLine;
        ClockFrequencyHz = clockFrequencyHz;
        Mode = mode;
        Device = new FakeSpiDevice(_chipId);
        return Device;
    }

    public void Dispose() => Disposed = true;
}

public sealed class FakeSpiDevice : ISpiDevice
{
    private const byte ReadBit = 0x80;

    private readonly byte[] _registers = new byte[256];

    public FakeSpiDevice(byte chipId = 0xD1)
    {
        _registers[0x00] = chipId;
    }

    public bool Disposed { get; private set; }

    public List<byte> ReadAddresses { get; } = new List<byte>();

    public List<byte> WrittenAddresses { get; } = new List<byte>();

    public void SetRegister(int address, byte value) => _registers[address] = value;

    public byte GetRegister(int address) => _registers[address];

    public void Read(Span<byte> buffer) => buffer.Clear();

    public void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 2)
            return;

        byte address = (byte)(buffer[0] & 0x7F);
        _registers[address] = buffer[1];
        WrittenAddresses.Add(address);
    }

    public void TransferFullDuplex(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
    {
        readBuffer.Clear();

        if (writeBuffer.Length == 0)
            return;

        byte rawAddress = writeBuffer[0];
        if ((rawAddress & ReadBit) == 0)
        {
            if (writeBuffer.Length >= 2)
            {
                byte writeAddress = (byte)(rawAddress & 0x7F);
                _registers[writeAddress] = writeBuffer[1];
                WrittenAddresses.Add(writeAddress);
            }

            return;
        }

        byte readAddress = (byte)(rawAddress & 0x7F);
        ReadAddresses.Add(readAddress);

        int dataLength = writeBuffer.Length - 2;
        for (int i = 0; i < dataLength && i + 2 < readBuffer.Length; i++)
            readBuffer[i + 2] = _registers[(readAddress + i) & 0xFF];
    }

    public void Dispose() => Disposed = true;
}

public sealed class FakeSerialTransport : ISerialTransport
{
    private readonly Queue<byte> _incoming = new Queue<byte>();

    public bool IsOpen { get; private set; }

    public bool Disposed { get; private set; }

    public int BytesToRead => _incoming.Count;

    public List<byte[]> Writes { get; } = new List<byte[]>();

    public void Feed(byte[] bytes)
    {
        foreach (byte value in bytes)
            _incoming.Enqueue(value);
    }

    public void Feed(string text) => Feed(System.Text.Encoding.ASCII.GetBytes(text));

    public void Open() => IsOpen = true;

    public void Close() => IsOpen = false;

    public byte[] ReadAvailable()
    {
        var result = _incoming.ToArray();
        _incoming.Clear();
        return result;
    }

    public void Write(byte[] bytes) => Writes.Add(bytes);

    public void Dispose()
    {
        IsOpen = false;
        Disposed = true;
    }
}

public sealed class FakePwmChannel : IPwmChannel
{
    public int Frequency { get; set; } = 50;

    public double DutyCycle { get; set; }

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public bool Disposed { get; private set; }

    public void Start() => StartCount++;

    public void Stop() => StopCount++;

    public void Dispose() => Disposed = true;
}

public sealed class FakeDShotTransport : IDShotTransport
{
    public List<ushort> Frames { get; } = new List<ushort>();

    public bool Disposed { get; private set; }

    public void SendFrame(ushort frame) => Frames.Add(frame);

    public void Dispose() => Disposed = true;
}
