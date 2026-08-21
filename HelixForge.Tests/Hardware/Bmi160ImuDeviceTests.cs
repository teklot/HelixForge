using System;
using System.Device.I2c;
using System.Threading;
using HelixForge.Hardware.Drivers;

namespace HelixForge.Tests.Hardware;

public class Bmi160ImuDeviceTests
{
    [Fact]
    public void Initialize_ValidChipId_Succeeds()
    {
        var fake = CreateFake();
        using var device = new Bmi160ImuDevice("imu-01", 1, 0x68, _ => fake);

        device.Initialize();

        Assert.True(device.IsInitialized);
    }

    [Fact]
    public void Initialize_InvalidChipId_Throws()
    {
        var fake = CreateFake(chipId: 0x42);
        using var device = new Bmi160ImuDevice("imu-01", 1, 0x68, _ => fake);

        Assert.Throws<InvalidOperationException>(() => device.Initialize());
    }

    [Fact]
    public void Read_GyroZ_UsesGyroDataRegistersNotReservedGap()
    {
        var fake = CreateFake();
        using var device = new Bmi160ImuDevice("imu-01", 1, 0x68, _ => fake);
        device.Initialize();

        fake.SetRegister(0x0A, 0x00);
        fake.SetRegister(0x0B, 0x00);
        fake.SetRegister(0x10, 0x00);
        fake.SetRegister(0x11, 0x40);

        ImuData reading = device.Read();

        Assert.Equal(1000.0, reading.AngularVelocity.Z, 3);
    }

    [Fact]
    public void Read_GyroXAndY_ParseFromGyroRegisters()
    {
        var fake = CreateFake();
        using var device = new Bmi160ImuDevice("imu-01", 1, 0x68, _ => fake);
        device.Initialize();

        fake.SetRegister(0x0C, 0x00);
        fake.SetRegister(0x0D, 0x10);
        fake.SetRegister(0x0E, 0x00);
        fake.SetRegister(0x0F, 0xF0);

        ImuData reading = device.Read();

        Assert.Equal(250.0, reading.AngularVelocity.X, 3);
        Assert.Equal(-250.0, reading.AngularVelocity.Y, 3);
    }

    [Fact]
    public void Read_ParsesAccelerometerFromBurstWindow()
    {
        var fake = CreateFake();
        using var device = new Bmi160ImuDevice("imu-01", 1, 0x68, _ => fake);
        device.Initialize();

        fake.SetRegister(0x04, 0x00);
        fake.SetRegister(0x05, 0x40);

        ImuData reading = device.Read();

        Assert.Equal(1.0, reading.Acceleration.X, 3);
    }

    [Fact]
    public void Read_TimestampAdvancesSinceInitialize()
    {
        var fake = CreateFake();
        using var device = new Bmi160ImuDevice("imu-01", 1, 0x68, _ => fake);
        device.Initialize();

        Thread.Sleep(20);
        ImuData reading = device.Read();

        Assert.True(reading.Timestamp > TimeSpan.Zero);
    }

    [Fact]
    public void Read_TimestampIsMonotonic()
    {
        var fake = CreateFake();
        using var device = new Bmi160ImuDevice("imu-01", 1, 0x68, _ => fake);
        device.Initialize();

        ImuData first = device.Read();
        Thread.Sleep(10);
        ImuData second = device.Read();

        Assert.True(second.Timestamp >= first.Timestamp);
    }

    private static FakeI2cDevice CreateFake(byte chipId = 0xD1)
    {
        return new FakeI2cDevice(1, 0x68, chipId);
    }
}
