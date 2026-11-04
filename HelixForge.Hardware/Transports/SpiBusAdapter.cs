using System;
using System.Collections.Generic;
using System.Device.Spi;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// <see cref="ISpiBus"/> implementation backed by <see cref="SpiDevice"/>.
/// </summary>
public sealed class SpiBusAdapter : ISpiBus
{
    private readonly int _busId;
    private readonly List<DeviceAdapter> _devices = new List<DeviceAdapter>();

    /// <summary>Creates an SPI bus adapter for the given bus ID (typically 0 on Raspberry Pi).</summary>
    /// <param name="busId">SPI bus ID.</param>
    public SpiBusAdapter(int busId)
    {
        _busId = busId;
    }

    /// <inheritdoc />
    public ISpiDevice OpenDevice(int chipSelectLine, int clockFrequencyHz, SpiMode mode)
    {
        var settings = new SpiConnectionSettings(_busId, chipSelectLine)
        {
            ClockFrequency = clockFrequencyHz,
            Mode = mode
        };

        var device = new DeviceAdapter(SpiDevice.Create(settings));
        _devices.Add(device);
        return device;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (DeviceAdapter device in _devices)
            device.Dispose();

        _devices.Clear();
    }

    private sealed class DeviceAdapter : ISpiDevice
    {
        private readonly SpiDevice _device;
        private bool _disposed;

        public DeviceAdapter(SpiDevice device)
        {
            _device = device;
        }

        public void Read(Span<byte> buffer) => _device.Read(buffer);

        public void Write(ReadOnlySpan<byte> buffer) => _device.Write(buffer);

        public void TransferFullDuplex(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
            => _device.TransferFullDuplex(writeBuffer, readBuffer);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _device.Dispose();
        }
    }
}
