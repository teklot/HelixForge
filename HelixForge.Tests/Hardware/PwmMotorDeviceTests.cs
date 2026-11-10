using System;
using HelixForge.Hardware.Drivers;

namespace HelixForge.Tests.Hardware;

public class PwmMotorDeviceTests
{
    [Fact]
    public void Initialize_StartsChannelAndMarksActive()
    {
        var channel = new FakePwmChannel();

        using var motor = new PwmMotorDevice("motor-01", channel, TimeSpan.Zero);
        motor.Initialize();

        Assert.Equal(1, channel.StartCount);
        Assert.True(motor.IsInitialized);
        Assert.True(motor.IsActive);
    }

    [Theory]
    [InlineData(0.0, 0.05)]
    [InlineData(0.5, 0.075)]
    [InlineData(1.0, 0.10)]
    public void SetThrottle_MapsToPulseWidthDutyCycle(double throttle, double expectedDutyCycle)
    {
        var channel = new FakePwmChannel();
        using var motor = new PwmMotorDevice("motor-01", channel, TimeSpan.Zero);
        motor.Initialize();

        motor.SetThrottle(throttle);

        Assert.Equal(expectedDutyCycle, channel.DutyCycle, 6);
        Assert.Equal(throttle, motor.Throttle);
    }

    [Fact]
    public void SetThrottle_BeforeInitialize_Throws()
    {
        var channel = new FakePwmChannel();
        using var motor = new PwmMotorDevice("motor-01", channel, TimeSpan.Zero);

        Assert.Throws<InvalidOperationException>(() => motor.SetThrottle(0.5));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void SetThrottle_OutOfRange_Throws(double throttle)
    {
        var channel = new FakePwmChannel();
        using var motor = new PwmMotorDevice("motor-01", channel, TimeSpan.Zero);
        motor.Initialize();

        Assert.Throws<ArgumentOutOfRangeException>(() => motor.SetThrottle(throttle));
    }

    [Fact]
    public void Reset_SetsZeroThrottle()
    {
        var channel = new FakePwmChannel();
        using var motor = new PwmMotorDevice("motor-01", channel, TimeSpan.Zero);
        motor.Initialize();
        motor.SetThrottle(0.8);

        motor.Reset();

        Assert.Equal(0.0, motor.Throttle);
        Assert.Equal(0.05, channel.DutyCycle, 6);
        Assert.True(motor.IsActive);
    }

    [Fact]
    public void Initialize_Twice_Throws()
    {
        var channel = new FakePwmChannel();
        using var motor = new PwmMotorDevice("motor-01", channel, TimeSpan.Zero);
        motor.Initialize();

        Assert.Throws<InvalidOperationException>(() => motor.Initialize());
    }

    [Fact]
    public void Dispose_DisarmsStopsAndDisposesChannel()
    {
        var channel = new FakePwmChannel();
        var motor = new PwmMotorDevice("motor-01", channel, TimeSpan.Zero);
        motor.Initialize();
        motor.SetThrottle(0.7);

        motor.Dispose();

        Assert.Equal(0.05, channel.DutyCycle, 6);
        Assert.Equal(1, channel.StopCount);
        Assert.True(channel.Disposed);
        Assert.Equal(0.0, motor.Throttle);
        Assert.False(motor.IsInitialized);
        Assert.False(motor.IsActive);
    }

    [Fact]
    public void Reset_AfterDispose_DoesNotThrowAndStaysInactive()
    {
        var channel = new FakePwmChannel();
        var motor = new PwmMotorDevice("motor-01", channel, TimeSpan.Zero);
        motor.Initialize();
        motor.SetThrottle(0.7);
        motor.Dispose();

        motor.Reset();

        Assert.Equal(0.0, motor.Throttle);
        Assert.False(motor.IsInitialized);
        Assert.False(motor.IsActive);
    }

    [Fact]
    public void Initialize_HonorsConfiguredArmDelay()
    {
        var channel = new FakePwmChannel();
        var motor = new PwmMotorDevice("motor-01", channel, TimeSpan.FromMilliseconds(20));
        var start = Environment.TickCount64;

        motor.Initialize();

        Assert.True(Environment.TickCount64 - start >= 15);
        motor.Dispose();
    }
}
