using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class SimMotorDeviceTests
{
    [Fact]
    public void Initialize_SetsIsActive()
    {
        var motor = new SimMotorDevice("motor-01", new MotorSimConfig());
        Assert.False(motor.IsActive);
        Assert.False(motor.IsInitialized);

        motor.Initialize();

        Assert.True(motor.IsActive);
        Assert.True(motor.IsInitialized);
    }

    [Fact]
    public void SetThrottle_UpdatesThrottle()
    {
        var config = new MotorSimConfig { ResponseTimeConstant = 0.01 };
        var motor = new SimMotorDevice("motor-01", config);
        motor.Initialize();

        motor.SetThrottle(0.75);
        motor.Update(TimeSpan.FromSeconds(0.1)); // Let it settle

        Assert.Equal(0.75, motor.Throttle, 2);
    }

    [Fact]
    public void SetThrottle_OutOfRange_Throws()
    {
        var motor = new SimMotorDevice("motor-01", new MotorSimConfig());
        motor.Initialize();

        Assert.Throws<ArgumentOutOfRangeException>(() => motor.SetThrottle(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => motor.SetThrottle(1.1));
    }

    [Fact]
    public void Update_ApplyResponseDynamics()
    {
        var config = new MotorSimConfig { ResponseTimeConstant = 0.05, MaxRpm = 10000 };
        var motor = new SimMotorDevice("motor-01", config);
        motor.Initialize();

        motor.SetThrottle(1.0);
        motor.Update(TimeSpan.FromSeconds(0.05)); // ~1 time constant

        // After 1 time constant, should reach ~63% of target
        Assert.InRange(motor.Throttle, 0.5, 0.8);
    }

    [Fact]
    public void CurrentRpm_ProportionalToThrottle()
    {
        var config = new MotorSimConfig { MaxRpm = 10000 };
        var motor = new SimMotorDevice("motor-01", config);
        motor.Initialize();

        motor.SetThrottle(0.5);
        motor.Update(TimeSpan.FromSeconds(1)); // Let it settle

        Assert.InRange(motor.CurrentRpm, 4900, 5100);
    }

    [Fact]
    public void Reset_ReturnsToInitialState()
    {
        var motor = new SimMotorDevice("motor-01", new MotorSimConfig());
        motor.Initialize();

        motor.SetThrottle(0.9);
        motor.Update(TimeSpan.FromSeconds(1));
        motor.Reset();

        Assert.Equal(0.0, motor.Throttle, 4);
    }

    [Fact]
    public void Deadband_ZeroesLowThrottle()
    {
        var config = new MotorSimConfig { Deadband = 0.1, ResponseTimeConstant = 0.01 };
        var motor = new SimMotorDevice("motor-01", config);
        motor.Initialize();

        motor.SetThrottle(0.05);
        motor.Update(TimeSpan.FromSeconds(0.1));

        Assert.Equal(0.0, motor.Throttle, 4);
    }
}
