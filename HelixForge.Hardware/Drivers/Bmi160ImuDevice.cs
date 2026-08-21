using System;
using System.Device.I2c;
using System.Diagnostics;

namespace HelixForge.Hardware.Drivers;

/// <summary>
/// Real IMU device driver for the Bosch BMI160 chip over I2C.
/// Provides accelerometer and gyroscope data via the IImuDevice interface.
/// </summary>
public sealed class Bmi160ImuDevice : IImuDevice
{
    private const int ChipIdRegister = 0x00;
    private const int ExpectedChipId = 0xD1;
    private const int AccelDataRegister = 0x04;
    private const int GyroDataRegister = 0x0C;
    private const int AccelConfigRegister = 0x40;
    private const int GyroConfigRegister = 0x42;
    private const int CommandRegister = 0x7E;
    private const int AccelNormalMode = 0x11;
    private const int GyroNormalMode = 0x15;

    private const double AccelScale = 2.0 / 32768.0;
    private const double GyroScale = 2000.0 / 32768.0;

    private readonly int _busId;
    private readonly int _deviceAddress;
    private readonly Func<I2cConnectionSettings, I2cDevice>? _deviceFactory;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private I2cDevice? _device;
    private ImuData _latestReading;

    /// <inheritdoc />
    public string DeviceId { get; }

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public ImuData LatestReading => _latestReading;

    /// <summary>Creates a new BMI160 IMU device.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="busId">I2C bus ID (typically 1 on Raspberry Pi).</param>
    /// <param name="deviceAddress">I2C address (0x68 or 0x69 depending on SDO pin).</param>
    /// <param name="deviceFactory">
    /// Optional factory used to create the I2C device. Defaults to <see cref="I2cDevice.Create"/>.
    /// Intended for custom transports and testing.
    /// </param>
    public Bmi160ImuDevice(string deviceId, int busId, int deviceAddress, Func<I2cConnectionSettings, I2cDevice>? deviceFactory = null)
    {
        DeviceId = deviceId;
        _busId = busId;
        _deviceAddress = deviceAddress;
        _deviceFactory = deviceFactory;
        _latestReading = ImuData.Empty;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        var settings = new I2cConnectionSettings(_busId, _deviceAddress);
        _device = _deviceFactory != null ? _deviceFactory(settings) : I2cDevice.Create(settings);

        byte chipId = ReadRegister(ChipIdRegister);
        if (chipId != ExpectedChipId)
            throw new InvalidOperationException(
                $"BMI160 at address 0x{_deviceAddress:X2} returned chip ID 0x{chipId:X2}, expected 0x{ExpectedChipId:X2}.");

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

        Span<byte> buffer = stackalloc byte[14];
        ReadRegisters(AccelDataRegister, buffer);

        short accelX = (short)(buffer[0] | (buffer[1] << 8));
        short accelY = (short)(buffer[2] | (buffer[3] << 8));
        short accelZ = (short)(buffer[4] | (buffer[5] << 8));
        short gyroX = (short)(buffer[8] | (buffer[9] << 8));
        short gyroY = (short)(buffer[10] | (buffer[11] << 8));
        short gyroZ = (short)(buffer[12] | (buffer[13] << 8));

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
        IsInitialized = false;
    }

    private byte ReadRegister(byte register)
    {
        Span<byte> writeBuf = stackalloc byte[] { register };
        Span<byte> readBuf = stackalloc byte[1];
        _device!.WriteRead(writeBuf, readBuf);
        return readBuf[0];
    }

    private void ReadRegisters(byte startRegister, Span<byte> buffer)
    {
        Span<byte> writeBuf = stackalloc byte[] { startRegister };
        _device!.WriteRead(writeBuf, buffer);
    }

    private void WriteRegister(byte register, byte value)
    {
        Span<byte> buffer = stackalloc byte[] { register, value };
        _device!.Write(buffer);
    }
}
