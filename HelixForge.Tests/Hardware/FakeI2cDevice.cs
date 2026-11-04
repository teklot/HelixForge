using System;
using System.Collections.Generic;
using HelixForge.Hardware.Transports;

namespace HelixForge.Tests.Hardware;

public sealed class FakeI2cBus : II2cBus
{
    private readonly byte _chipId;

    public FakeI2cBus(byte chipId = 0xD1)
    {
        _chipId = chipId;
    }

    public FakeI2cDevice? Device { get; private set; }

    public bool Disposed { get; private set; }

    public II2cDevice OpenDevice(int deviceAddress)
    {
        Device = new FakeI2cDevice(deviceAddress, _chipId);
        return Device;
    }

    public void Dispose() => Disposed = true;
}

public sealed class FakeI2cDevice : II2cDevice
{
    private readonly byte[] _registers = new byte[256];

    public FakeI2cDevice(int deviceAddress, byte chipId)
    {
        DeviceAddress = deviceAddress;
        _registers[0x00] = chipId;
    }

    public int DeviceAddress { get; }

    public bool Disposed { get; private set; }

    public void SetRegister(int address, byte value) => _registers[address] = value;

    public byte GetRegister(int address) => _registers[address];

    public void Read(Span<byte> buffer)
    {
        buffer.Clear();
    }

    public void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length >= 2)
            _registers[buffer[0]] = buffer[1];
    }

    public void WriteRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
    {
        int startRegister = writeBuffer[0];
        for (int i = 0; i < readBuffer.Length; i++)
            readBuffer[i] = _registers[(startRegister + i) & 0xFF];
    }

    public void Dispose() => Disposed = true;
}
