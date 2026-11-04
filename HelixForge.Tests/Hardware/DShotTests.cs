using System;
using HelixForge.Hardware.Drivers;

namespace HelixForge.Tests.Hardware;

public class DShotCodecTests
{
    [Fact]
    public void Encode_Zero_IsDisarmedFrame()
    {
        Assert.Equal(0, DShotCodec.Encode(0.0));
    }

    [Theory]
    [InlineData(0.0001, DShotCodec.MinThrottleValue)]
    [InlineData(0.25, 548)]
    [InlineData(0.5, 1048)]
    [InlineData(1.0, DShotCodec.MaxThrottleValue)]
    public void Encode_MapsNormalizedThrottleIntoThrottleRange(double throttle, int expectedValue)
    {
        ushort frame = DShotCodec.Encode(throttle);

        Assert.True(DShotCodec.TryDecode(frame, out int value, out bool telemetryRequest));
        Assert.Equal(expectedValue, value);
        Assert.False(telemetryRequest);
    }

    [Fact]
    public void Encode_SetsTelemetryBit()
    {
        ushort frame = DShotCodec.Encode(0.5, telemetryRequest: true);

        Assert.True(DShotCodec.TryDecode(frame, out int value, out bool telemetryRequest));
        Assert.Equal(1048, value);
        Assert.True(telemetryRequest);
        Assert.True(DShotCodec.IsValid(frame));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Encode_OutOfRange_Throws(double throttle)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DShotCodec.Encode(throttle));
    }

    [Theory]
    [InlineData(0x000, 0x0)]
    [InlineData(0xABC, 0xD)]
    public void ComputeCrc_MatchesDShotPolynomial(int value, int expected)
    {
        Assert.Equal(expected, DShotCodec.ComputeCrc(value));
    }

    [Fact]
    public void IsValid_RejectsCorruptedFrame()
    {
        ushort frame = (ushort)(DShotCodec.Encode(0.5) ^ 0x0001);

        Assert.False(DShotCodec.IsValid(frame));
        Assert.False(DShotCodec.TryDecode(frame, out _, out _));
    }
}

public class DShotMotorDeviceTests
{
    [Fact]
    public void Initialize_SendsDisarmedFrame()
    {
        var transport = new FakeDShotTransport();
        using var motor = new DShotMotorDevice("esc-01", transport);

        motor.Initialize();

        Assert.Equal(new ushort[] { 0 }, transport.Frames);
        Assert.True(motor.IsInitialized);
        Assert.True(motor.IsActive);
    }

    [Fact]
    public void SetThrottle_SendsEncodedFrame()
    {
        var transport = new FakeDShotTransport();
        using var motor = new DShotMotorDevice("esc-01", transport);
        motor.Initialize();

        motor.SetThrottle(0.5);

        Assert.Equal(DShotCodec.Encode(0.5), transport.Frames[1]);
        Assert.Equal(0.5, motor.Throttle);
    }

    [Fact]
    public void SetThrottle_TelemetryRequested_SetsTelemetryBit()
    {
        var transport = new FakeDShotTransport();
        using var motor = new DShotMotorDevice("esc-01", transport, telemetryRequested: true);
        motor.Initialize();

        motor.SetThrottle(0.2);

        Assert.True(DShotCodec.TryDecode(transport.Frames[1], out _, out bool telemetryRequest));
        Assert.True(telemetryRequest);
    }

    [Fact]
    public void SetThrottle_BeforeInitialize_Throws()
    {
        var transport = new FakeDShotTransport();
        using var motor = new DShotMotorDevice("esc-01", transport);

        Assert.Throws<InvalidOperationException>(() => motor.SetThrottle(0.5));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void SetThrottle_OutOfRange_Throws(double throttle)
    {
        var transport = new FakeDShotTransport();
        using var motor = new DShotMotorDevice("esc-01", transport);
        motor.Initialize();

        Assert.Throws<ArgumentOutOfRangeException>(() => motor.SetThrottle(throttle));
    }

    [Fact]
    public void Reset_SendsDisarmFrame()
    {
        var transport = new FakeDShotTransport();
        using var motor = new DShotMotorDevice("esc-01", transport);
        motor.Initialize();
        motor.SetThrottle(0.8);

        motor.Reset();

        Assert.Equal(0, transport.Frames[transport.Frames.Count - 1]);
        Assert.Equal(0.0, motor.Throttle);
    }

    [Fact]
    public void Initialize_Twice_Throws()
    {
        var transport = new FakeDShotTransport();
        using var motor = new DShotMotorDevice("esc-01", transport);
        motor.Initialize();

        Assert.Throws<InvalidOperationException>(() => motor.Initialize());
    }

    [Fact]
    public void Dispose_SendsDisarmFrameAndDisposesTransport()
    {
        var transport = new FakeDShotTransport();
        var motor = new DShotMotorDevice("esc-01", transport);
        motor.Initialize();
        motor.SetThrottle(0.6);

        motor.Dispose();

        Assert.Equal(0, transport.Frames[transport.Frames.Count - 1]);
        Assert.True(transport.Disposed);
        Assert.False(motor.IsInitialized);
        Assert.False(motor.IsActive);
    }
}
