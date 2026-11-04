using System;
using System.Device.Spi;
using System.Diagnostics;
using HelixForge.Hardware.Transports;

namespace HelixForge.Hardware.Drivers;

/// <summary>
/// Real IMU device driver for the Bosch BMI160 chip over SPI.
/// Provides accelerometer and gyroscope data via the IImuDevice interface.
/// </summary>
public sealed class Bmi160SpiImuDevice : IImuDevice
{
    private const byte ChipIdRegister = 0x00;
    private const byte ExpectedChipId = 0xD1;
    private const byte DataRegister = 0x0C;
    private const int DataLength = 12;
    private const byte AccelConfigRegister = 0x40;
    private const byte GyroConfigRegister = 0x42;
    private const byte CommandRegister = 0x7E;
    private const byte AccelNormalMode = 0x11;
    private const byte GyroNormalMode = 0x15;
    private const byte SpiReadBit = 0x80;
    private const byte SpiWriteMask = 0x7F;
    private const byte SpiEnableRegister = 0x7F;
    private const int DefaultClockFrequencyHz = 10_000_000;

    private const double AccelScale = 2.0 / 32768.0;
    private const double GyroScale = 2000.0 / 32768.0;

    private readonly int _busId;
    private readonly int _chipSelectLine;
    private readonly int _clockFrequencyHz;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private ISpiBus? _ownedBus;
    private ISpiDevice? _device;
    private ImuData _latestReading;

    /// <inheritdoc />
    public string DeviceId { get; }

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public ImuData LatestReading => _latestReading;

    /// <summary>Creates a new BMI160 IMU device on a physical SPI bus.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="busId">SPI bus ID (typically 0 on Raspberry Pi).</param>
    /// <param name="chipSelectLine">Chip-select line number.</param>
    /// <param name="clockFrequencyHz">SPI clock frequency; the BMI160 supports up to 10 MHz.</param>
    public Bmi160SpiImuDevice(string deviceId, int busId, int chipSelectLine, int clockFrequencyHz = DefaultClockFrequencyHz)
    {
        DeviceId = deviceId;
        _busId = busId;
        _chipSelectLine = chipSelectLine;
        _clockFrequencyHz = clockFrequencyHz;
        _latestReading = ImuData.Empty;
    }

    /// <summary>Creates a new BMI160 IMU device on a supplied SPI device.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="device">SPI device already connected to the chip. The driver takes ownership of it.</param>
    public Bmi160SpiImuDevice(string deviceId, ISpiDevice device)
    {
        DeviceId = deviceId;
        _device = device;
        _latestReading = ImuData.Empty;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        if (_device == null)
        {
            _ownedBus = new SpiBusAdapter(_busId);
            _device = _ownedBus.OpenDevice(_chipSelectLine, _clockFrequencyHz, SpiMode.Mode0);
        }

        Span<byte> enable = stackalloc byte[1];
        ReadRegisters(SpiEnableRegister, enable);

        byte chipId = ReadRegister(ChipIdRegister);
        if (chipId != ExpectedChipId)
            throw new InvalidOperationException(
                $"BMI160 returned chip ID 0x{chipId:X2}, expected 0x{ExpectedChipId:X2}.");

        WriteRegister(AccelConfigRegister, 0x28);
        WriteRegister(GyroConfigRegister, 0x28);
        WriteRegister(CommandRegister, AccelNormalMode);
        WriteRegister(CommandRegister, GyroNormalMode);

        _stopwatch.Restart();
        IsInitialized = true;
    }

    /// <inheritdoc />
    public ImuData Read()
    {
        if (!IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is not initialized.");

        Span<byte> buffer = stackalloc byte[DataLength];
        ReadRegisters(DataRegister, buffer);

        short gyroX = ReadInt16(buffer, 0);
        short gyroY = ReadInt16(buffer, 2);
        short gyroZ = ReadInt16(buffer, 4);
        short accelX = ReadInt16(buffer, 6);
        short accelY = ReadInt16(buffer, 8);
        short accelZ = ReadInt16(buffer, 10);

        var acceleration = new Vector3(accelX * AccelScale, accelY * AccelScale, accelZ * AccelScale);
        var angularVelocity = new Vector3(gyroX * GyroScale, gyroY * GyroScale, gyroZ * GyroScale);

        _latestReading = new ImuData(Vector3.Zero, angularVelocity, acceleration, _stopwatch.Elapsed);
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
        _latestReading = ImuData.Empty;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopwatch.Reset();
        _device?.Dispose();
        _device = null;
        _ownedBus?.Dispose();
        _ownedBus = null;
        IsInitialized = false;
    }

    private byte ReadRegister(byte register)
    {
        Span<byte> result = stackalloc byte[1];
        ReadRegisters(register, result);
        return result[0];
    }

    private void ReadRegisters(byte startRegister, Span<byte> buffer)
    {
        Span<byte> writeBuffer = stackalloc byte[buffer.Length + 2];
        Span<byte> readBuffer = stackalloc byte[buffer.Length + 2];
        writeBuffer.Clear();
        writeBuffer[0] = (byte)(startRegister | SpiReadBit);

        _device!.TransferFullDuplex(writeBuffer, readBuffer);

        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = readBuffer[i + 2];
    }

    private void WriteRegister(byte register, byte value)
    {
        Span<byte> buffer = stackalloc byte[] { (byte)(register & SpiWriteMask), value };
        _device!.Write(buffer);
    }

    private static short ReadInt16(Span<byte> buffer, int offset)
    {
        return (short)(buffer[offset] | (buffer[offset + 1] << 8));
    }
}
