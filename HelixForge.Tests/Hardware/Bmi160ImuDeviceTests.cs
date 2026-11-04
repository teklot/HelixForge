using System;
using System.Threading;
using HelixForge.Hardware.Drivers;

namespace HelixForge.Tests.Hardware;

public class Bmi160ImuDeviceTests
{
    [Fact]
    public void Initialize_ValidChipId_Succeeds()
    {
        var bus = new FakeI2cBus();
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);

        device.Initialize();

        Assert.True(device.IsInitialized);
        Assert.NotNull(bus.Device);
        Assert.Equal(0x68, bus.Device!.DeviceAddress);
    }

    [Fact]
    public void Initialize_InvalidChipId_Throws()
    {
        var bus = new FakeI2cBus(chipId: 0x42);
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);

        Assert.Throws<InvalidOperationException>(() => device.Initialize());
    }

    [Fact]
    public void Read_GyroZ_UsesGyroDataRegistersNotReservedGap()
    {
        var bus = new FakeI2cBus();
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);
        device.Initialize();

        bus.Device!.SetRegister(0x0A, 0x00);
        bus.Device.SetRegister(0x0B, 0x00);
        bus.Device.SetRegister(0x10, 0x00);
        bus.Device.SetRegister(0x11, 0x40);

        ImuData reading = device.Read();

        Assert.Equal(1000.0, reading.AngularVelocity.Z, 3);
    }

    [Fact]
    public void Read_GyroXAndY_ParseFromGyroRegisters()
    {
        var bus = new FakeI2cBus();
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);
        device.Initialize();

        bus.Device!.SetRegister(0x0C, 0x00);
        bus.Device.SetRegister(0x0D, 0x10);
        bus.Device.SetRegister(0x0E, 0x00);
        bus.Device.SetRegister(0x0F, 0xF0);

        ImuData reading = device.Read();

        Assert.Equal(250.0, reading.AngularVelocity.X, 3);
        Assert.Equal(-250.0, reading.AngularVelocity.Y, 3);
    }

    [Fact]
    public void Read_Acceleration_ParsesFromAccelRegisters()
    {
        var bus = new FakeI2cBus();
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);
        device.Initialize();

        bus.Device!.SetRegister(0x12, 0x00);
        bus.Device.SetRegister(0x13, 0x40);
        bus.Device.SetRegister(0x14, 0x00);
        bus.Device.SetRegister(0x15, 0xC0);
        bus.Device.SetRegister(0x16, 0x00);
        bus.Device.SetRegister(0x17, 0x80);

        ImuData reading = device.Read();

        Assert.Equal(1.0, reading.Acceleration.X, 3);
        Assert.Equal(-1.0, reading.Acceleration.Y, 3);
        Assert.Equal(-2.0, reading.Acceleration.Z, 3);
    }

    [Fact]
    public void Read_Acceleration_IgnoresMagnetometerRegisters()
    {
        var bus = new FakeI2cBus();
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);
        device.Initialize();

        for (int register = 0x04; register <= 0x09; register++)
            bus.Device!.SetRegister(register, 0x7F);

        ImuData reading = device.Read();

        Assert.Equal(0.0, reading.Acceleration.X, 3);
        Assert.Equal(0.0, reading.Acceleration.Y, 3);
        Assert.Equal(0.0, reading.Acceleration.Z, 3);
    }

    [Fact]
    public void Initialize_WritesExpectedConfiguration()
    {
        var bus = new FakeI2cBus();
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);

        device.Initialize();

        Assert.Equal(0x28, bus.Device!.GetRegister(0x40));
        Assert.Equal(0x28, bus.Device.GetRegister(0x42));
        Assert.Equal(0x15, bus.Device.GetRegister(0x7E));
    }

    [Fact]
    public void Read_BeforeInitialize_Throws()
    {
        var bus = new FakeI2cBus();
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);

        Assert.Throws<InvalidOperationException>(() => device.Read());
    }

    [Fact]
    public void Initialize_Twice_Throws()
    {
        var bus = new FakeI2cBus();
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);
        device.Initialize();

        Assert.Throws<InvalidOperationException>(() => device.Initialize());
    }

    [Fact]
    public void Dispose_DisposesDeviceButNotCallerBus()
    {
        var bus = new FakeI2cBus();
        var device = new Bmi160ImuDevice("imu-01", 0x68, bus);
        device.Initialize();

        device.Dispose();

        Assert.False(bus.Disposed);
        Assert.True(bus.Device!.Disposed);
        Assert.False(device.IsInitialized);
    }

    [Fact]
    public void Read_TimestampAdvancesSinceInitialize()
    {
        var bus = new FakeI2cBus();
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);
        device.Initialize();

        Thread.Sleep(20);
        ImuData reading = device.Read();

        Assert.True(reading.Timestamp > TimeSpan.Zero);
    }

    [Fact]
    public void Read_TimestampIsMonotonic()
    {
        var bus = new FakeI2cBus();
        using var device = new Bmi160ImuDevice("imu-01", 0x68, bus);
        device.Initialize();

        ImuData first = device.Read();
        Thread.Sleep(10);
        ImuData second = device.Read();

        Assert.True(second.Timestamp >= first.Timestamp);
    }
}
