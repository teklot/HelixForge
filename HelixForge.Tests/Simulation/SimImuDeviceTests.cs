using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class SimImuDeviceTests
{
    [Fact]
    public void Initialize_SetsIsInitialized()
    {
        var imu = new SimImuDevice("imu-01", new ImuSimConfig());
        Assert.False(imu.IsInitialized);

        imu.Initialize();

        Assert.True(imu.IsInitialized);
    }

    [Fact]
    public void Initialize_CalledTwice_Throws()
    {
        var imu = new SimImuDevice("imu-01", new ImuSimConfig());
        imu.Initialize();

        Assert.Throws<InvalidOperationException>(() => imu.Initialize());
    }

    [Fact]
    public void Read_BeforeInitialize_ReturnsEmpty()
    {
        var imu = new SimImuDevice("imu-01", new ImuSimConfig());
        var data = imu.Read();
        Assert.Equal(ImuData.Empty, data);
    }

    [Fact]
    public void Update_AdvancesTime()
    {
        var imu = new SimImuDevice("imu-01", new ImuSimConfig());
        imu.Initialize();

        imu.Update(TimeSpan.FromSeconds(1));

        var data = imu.Read();
        Assert.Equal(TimeSpan.FromSeconds(1), data.Timestamp);
    }

    [Fact]
    public void Update_AppliesNoise()
    {
        var config = new ImuSimConfig { AccelerometerNoise = 0.1, RandomSeed = 42 };
        var imu = new SimImuDevice("imu-01", config);
        imu.Initialize();

        imu.Update(TimeSpan.FromSeconds(1));

        var data = imu.Read();
        // With noise, acceleration should differ from pure gravity
        Assert.NotEqual(0.0, data.Acceleration.X, 4);
    }

    [Fact]
    public void SetAngularVelocity_AffectsOrientation()
    {
        var config = new ImuSimConfig { InitialOrientation = Vector3.Zero };
        var imu = new SimImuDevice("imu-01", config);
        imu.Initialize();

        imu.SetAngularVelocity(new Vector3(1.0, 0.0, 0.0));
        imu.Update(TimeSpan.FromSeconds(1));

        var data = imu.Read();
        Assert.Equal(1.0, data.Orientation.X, 4);
    }

    [Fact]
    public void Reset_ReturnsToInitialState()
    {
        var config = new ImuSimConfig { InitialOrientation = new Vector3(0.5, 0, 0) };
        var imu = new SimImuDevice("imu-01", config);
        imu.Initialize();

        imu.SetAngularVelocity(new Vector3(1.0, 0.0, 0.0));
        imu.Update(TimeSpan.FromSeconds(1));
        imu.Reset();

        var data = imu.Read();
        Assert.Equal(0.5, data.Orientation.X, 4);
    }

    [Fact]
    public void DeviceId_ReturnsCorrectId()
    {
        var imu = new SimImuDevice("my-imu", new ImuSimConfig());
        Assert.Equal("my-imu", imu.DeviceId);
    }

    [Fact]
    public void BiasInstability_AccumulatesOverTime()
    {
        var config = new ImuSimConfig
        {
            AccelerometerNoise = 0.0,
            GyroscopeNoise = 0.0,
            BiasInstability = 0.1,
            RandomSeed = 42
        };
        var imu = new SimImuDevice("imu-01", config);
        imu.Initialize();

        imu.Update(TimeSpan.FromSeconds(1));

        var data = imu.Read();
        // Bias instability random-walks the gyro readings away from their true value (0)
        Assert.NotEqual(0.0, data.AngularVelocity.X, 4);
    }

    [Fact]
    public void TemperatureCoefficient_AddsGyroBias()
    {
        var config = new ImuSimConfig
        {
            AccelerometerNoise = 0.0,
            GyroscopeNoise = 0.0,
            TemperatureCoefficient = 0.01,
            OperatingTemperature = 25.0,
            Temperature = 50.0, // 25 deg above operating point
            RandomSeed = 42
        };
        var imu = new SimImuDevice("imu-01", config);
        imu.Initialize();

        imu.Update(TimeSpan.FromSeconds(1));

        var data = imu.Read();
        // 25 deg * 0.01 = 0.25 rad/s gyro bias (plus noise=0)
        Assert.InRange(data.AngularVelocity.X, 0.2, 0.3);
    }

    [Fact]
    public void TemperatureCoefficient_AtOperatingPoint_AddsNoBias()
    {
        var config = new ImuSimConfig
        {
            AccelerometerNoise = 0.0,
            GyroscopeNoise = 0.0,
            AccelerometerDrift = 0.0,
            GyroscopeDrift = 0.0,
            TemperatureCoefficient = 0.01,
            OperatingTemperature = 25.0,
            Temperature = 25.0, // at operating point
            RandomSeed = 42
        };
        var imu = new SimImuDevice("imu-01", config);
        imu.Initialize();

        imu.Update(TimeSpan.FromSeconds(1));

        var data = imu.Read();
        Assert.Equal(0.0, data.AngularVelocity.X, 4);
    }
}
