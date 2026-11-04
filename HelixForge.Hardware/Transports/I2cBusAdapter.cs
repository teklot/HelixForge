using System;
using System.Device.I2c;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// <see cref="II2cBus"/> implementation backed by <see cref="I2cBus"/>.
/// </summary>
public sealed class I2cBusAdapter : II2cBus
{
    private readonly I2cBus _bus;

    /// <summary>Opens the I2C bus with the given bus ID (typically 1 on Raspberry Pi).</summary>
    /// <param name="busId">I2C bus ID.</param>
    public I2cBusAdapter(int busId)
    {
        _bus = I2cBus.Create(busId);
    }

    /// <inheritdoc />
    public II2cDevice OpenDevice(int deviceAddress)
    {
        return new DeviceAdapter(_bus.CreateDevice(deviceAddress));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _bus.Dispose();
    }

    private sealed class DeviceAdapter : II2cDevice
    {
        private readonly I2cDevice _device;

        public DeviceAdapter(I2cDevice device)
        {
            _device = device;
        }

        public int DeviceAddress => _device.ConnectionSettings.DeviceAddress;

        public void Read(Span<byte> buffer) => _device.Read(buffer);

        public void Write(ReadOnlySpan<byte> buffer) => _device.Write(buffer);

        public void WriteRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
            => _device.WriteRead(writeBuffer, readBuffer);

        public void Dispose() => _device.Dispose();
    }
}
