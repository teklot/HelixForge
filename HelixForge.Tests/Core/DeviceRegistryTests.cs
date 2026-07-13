using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Core;

public class DeviceRegistryTests
{
    [Fact]
    public void Register_AddsDevice()
    {
        var registry = new DeviceRegistry();
        var device = new SimImuDevice("imu-01", new ImuSimConfig());

        registry.Register(device);

        Assert.Equal(1, registry.Count);
        Assert.True(registry.Contains("imu-01"));
    }

    [Fact]
    public void Register_DuplicateId_Throws()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));

        Assert.Throws<InvalidOperationException>(() =>
            registry.Register(new SimImuDevice("imu-01", new ImuSimConfig())));
    }

    [Fact]
    public void Register_Null_Throws()
    {
        var registry = new DeviceRegistry();
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public void GetById_ReturnsDevice()
    {
        var registry = new DeviceRegistry();
        var device = new SimImuDevice("imu-01", new ImuSimConfig());
        registry.Register(device);

        var result = registry.GetById("imu-01");
        Assert.Same(device, result);
    }

    [Fact]
    public void GetById_NotFound_ReturnsNull()
    {
        var registry = new DeviceRegistry();
        Assert.Null(registry.GetById("nonexistent"));
    }

    [Fact]
    public void GetByType_ReturnsFirstDevice()
    {
        var registry = new DeviceRegistry();
        var imu = new SimImuDevice("imu-01", new ImuSimConfig());
        registry.Register(imu);

        var result = registry.GetByType<IImuDevice>();
        Assert.Same(imu, result);
    }

    [Fact]
    public void GetByType_NoDevices_ReturnsNull()
    {
        var registry = new DeviceRegistry();
        Assert.Null(registry.GetByType<IImuDevice>());
    }

    [Fact]
    public void GetAllByType_ReturnsAllMatchingDevices()
    {
        var registry = new DeviceRegistry();
        var imu1 = new SimImuDevice("imu-01", new ImuSimConfig());
        var imu2 = new SimImuDevice("imu-02", new ImuSimConfig());
        var motor = new SimMotorDevice("motor-01", new MotorSimConfig());

        registry.Register(imu1);
        registry.Register(imu2);
        registry.Register(motor);

        var imus = registry.GetAllByType<IImuDevice>().ToList();
        Assert.Equal(2, imus.Count);
    }

    [Fact]
    public void Unregister_RemovesDevice()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));

        bool result = registry.Unregister("imu-01");

        Assert.True(result);
        Assert.Equal(0, registry.Count);
        Assert.False(registry.Contains("imu-01"));
    }

    [Fact]
    public void Unregister_NotFound_ReturnsFalse()
    {
        var registry = new DeviceRegistry();
        Assert.False(registry.Unregister("nonexistent"));
    }

    [Fact]
    public void GetAllDevices_ReturnsAllRegistered()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));
        registry.Register(new SimMotorDevice("motor-01", new MotorSimConfig()));

        var all = registry.GetAllDevices();
        Assert.Equal(2, all.Count);
    }
}
