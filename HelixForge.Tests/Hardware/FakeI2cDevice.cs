using System;
using System.Device.I2c;

namespace HelixForge.Tests.Hardware;

public sealed class FakeI2cDevice : I2cDevice
{
    private readonly byte[] _registers = new byte[256];

    public FakeI2cDevice(int busId, int deviceAddress, byte chipId)
    {
        ConnectionSettings = new I2cConnectionSettings(busId, deviceAddress);
        _registers[0x00] = chipId;
    }

    public override I2cConnectionSettings ConnectionSettings { get; }

    public void SetRegister(int address, byte value) => _registers[address] = value;

    public override void Read(Span<byte> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = 0;
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length >= 2)
            _registers[buffer[0]] = buffer[1];
    }

    public override void WriteRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
    {
        int startRegister = writeBuffer[0];
        for (int i = 0; i < readBuffer.Length; i++)
            readBuffer[i] = _registers[(startRegister + i) & 0xFF];
    }
}
