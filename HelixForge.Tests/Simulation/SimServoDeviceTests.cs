using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class SimServoDeviceTests
{
    [Fact]
    public void Initialize_SetsIsActive()
    {
        var servo = new SimServoDevice("servo-01", new ServoSimConfig());
        Assert.False(servo.IsActive);
        Assert.False(servo.IsInitialized);

        servo.Initialize();

        Assert.True(servo.IsActive);
        Assert.True(servo.IsInitialized);
    }

    [Fact]
    public void SetAngle_ClampsToLimits()
    {
        var config = new ServoSimConfig { MinAngle = 0.0, MaxAngle = 180.0 };
        var servo = new SimServoDevice("servo-01", config);
        servo.Initialize();

        servo.SetAngle(240.0);

        Assert.Equal(180.0, servo.TargetAngle, 4);
    }

    [Fact]
    public void SetAngle_ClampsLowLimit()
    {
        var config = new ServoSimConfig { MinAngle = -45.0, MaxAngle = 90.0 };
        var servo = new SimServoDevice("servo-01", config);
        servo.Initialize();

        servo.SetAngle(-100.0);

        Assert.Equal(-45.0, servo.TargetAngle, 4);
    }

    [Fact]
    public void Update_SlewsTowardTarget()
    {
        var config = new ServoSimConfig { SlewRate = 100.0, MaxAngle = 180.0 };
        var servo = new SimServoDevice("servo-01", config);
        servo.Initialize();

        servo.SetAngle(180.0);
        servo.Update(TimeSpan.FromSeconds(0.5));

        // 0.5s at 100 deg/s = 50 degrees
        Assert.Equal(50.0, servo.Angle, 4);
    }

    [Fact]
    public void Update_ReachesTargetOverTime()
    {
        var config = new ServoSimConfig { SlewRate = 100.0, MaxAngle = 180.0, InitialAngle = 90.0 };
        var servo = new SimServoDevice("servo-01", config);
        servo.Initialize();

        servo.SetAngle(180.0);
        servo.Update(TimeSpan.FromSeconds(10)); // Should reach target

        Assert.Equal(180.0, servo.Angle, 4);
        Assert.Equal(servo.Angle, servo.TargetAngle, 4);
    }

    [Fact]
    public void Reset_ReturnsToInitialState()
    {
        var config = new ServoSimConfig { InitialAngle = 30.0, SlewRate = 100.0 };
        var servo = new SimServoDevice("servo-01", config);
        servo.Initialize();

        servo.SetAngle(120.0);
        servo.Update(TimeSpan.FromSeconds(10));
        servo.Reset();

        Assert.Equal(30.0, servo.Angle, 4);
    }

    [Fact]
    public void DeviceId_ReturnsCorrectId()
    {
        var servo = new SimServoDevice("my-servo", new ServoSimConfig());
        Assert.Equal("my-servo", servo.DeviceId);
    }
}
