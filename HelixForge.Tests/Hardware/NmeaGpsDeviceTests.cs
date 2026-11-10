using System;
using System.Globalization;
using HelixForge.Hardware.Drivers;
using HelixForge.Hardware.Transports;

namespace HelixForge.Tests.Hardware;

public class NmeaGpsDeviceTests
{
    private const string GgaSentence = "$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47\r\n";
    private const string RmcSentence = "$GPRMC,123519,A,4807.038,N,01131.000,E,022.4,084.4,230394,003.1,W*6A\r\n";

    [Fact]
    public void Initialize_OpensTransport()
    {
        var transport = new FakeSerialTransport();
        using var device = new NmeaGpsDevice("gps-01", transport);

        device.Initialize();

        Assert.True(transport.IsOpen);
        Assert.True(device.IsInitialized);
    }

    [Fact]
    public void Read_ParsesGgaPositionAndAltitude()
    {
        var (device, transport) = CreateDevice();
        transport.Feed(GgaSentence);

        GpsData reading = device.Read();

        Assert.Equal(48.1173, reading.Latitude, 4);
        Assert.Equal(11.5166667, reading.Longitude, 4);
        Assert.Equal(545.4, reading.Altitude, 3);
    }

    [Theory]
    [InlineData(0, GpsFixStatus.NoFix)]
    [InlineData(1, GpsFixStatus.Fix3D)]
    [InlineData(2, GpsFixStatus.Fix3D)]
    [InlineData(4, GpsFixStatus.Fix3D)]
    [InlineData(5, GpsFixStatus.Fix3D)]
    public void Read_MapsGgaFixQualityToFixStatus(int quality, GpsFixStatus expected)
    {
        var (device, transport) = CreateDevice();
        transport.Feed(GgaLine(quality));

        GpsData reading = device.Read();

        Assert.Equal(expected, reading.FixStatus);
    }

    [Fact]
    public void Read_ParsesRmcSpeedAndCourseAsNorthEastVelocity()
    {
        var (device, transport) = CreateDevice();
        transport.Feed(GgaSentence);
        transport.Feed(RmcSentence);

        GpsData reading = device.Read();

        double speedMps = 22.4 * 0.514444;
        double courseRadians = 84.4 * Math.PI / 180.0;
        Assert.Equal(speedMps * Math.Cos(courseRadians), reading.Velocity.X, 4);
        Assert.Equal(speedMps * Math.Sin(courseRadians), reading.Velocity.Y, 4);
        Assert.Equal(0.0, reading.Velocity.Z, 3);
    }

    [Theory]
    [InlineData(0.0, 11.5235, 0.0)]
    [InlineData(90.0, 0.0, 11.5235)]
    [InlineData(180.0, -11.5235, 0.0)]
    [InlineData(270.0, 0.0, -11.5235)]
    public void Read_ConvertsRmcCourseToNorthEastVelocity(double course, double expectedNorth, double expectedEast)
    {
        var (device, transport) = CreateDevice();
        transport.Feed(RmcLine(course));

        GpsData reading = device.Read();

        Assert.Equal(expectedNorth, reading.Velocity.X, 3);
        Assert.Equal(expectedEast, reading.Velocity.Y, 3);
    }

    [Fact]
    public void Read_GgaAfterRmc_PreservesVelocity()
    {
        var (device, transport) = CreateDevice();
        transport.Feed(RmcLine(90.0));
        GpsData afterRmc = device.Read();

        transport.Feed(GgaLine(1));
        GpsData afterGga = device.Read();

        Assert.Equal(afterRmc.Velocity, afterGga.Velocity);
        Assert.Equal(GpsFixStatus.Fix3D, afterGga.FixStatus);
    }

    [Fact]
    public void Read_RmcWithoutCourse_LeavesReadingUnchanged()
    {
        var (device, transport) = CreateDevice();
        transport.Feed(GgaLine(1));
        GpsData before = device.Read();

        transport.Feed("$GPRMC,123519,A,4807.038,N,01131.000,E,022.4,,230394,003.1,W\r\n");
        GpsData after = device.Read();

        Assert.Equal(before.Velocity, after.Velocity);
        Assert.Equal(before.FixStatus, after.FixStatus);
    }

    [Fact]
    public void Read_HandlesSentenceSplitAcrossReads()
    {
        var (device, transport) = CreateDevice();
        transport.Feed("$GNGGA,123519,4807.038,N,0");

        GpsData partial = device.Read();
        Assert.Equal(0.0, partial.Latitude);

        transport.Feed("1131.000,E,1,08,0.9,545.4,M,46.9,M,,*48\r\n");
        GpsData complete = device.Read();

        Assert.Equal(48.1173, complete.Latitude, 4);
        Assert.Equal(11.5166667, complete.Longitude, 4);
    }

    [Fact]
    public void Read_IgnoresUnknownAndMalformedSentences()
    {
        var (device, transport) = CreateDevice();
        transport.Feed("$GPGSV,3,1,11,03,03,111,00*71\r\n");
        transport.Feed("$GPGGA,bad,data\n");

        GpsData reading = device.Read();

        Assert.Equal(GpsData.Empty, reading);
    }

    [Fact]
    public void Read_SouthAndWestDirectionsAreNegative()
    {
        var (device, transport) = CreateDevice();
        transport.Feed("$GPGGA,123519,4807.038,S,01131.000,W,1,08,0.9,545.4,M,46.9,M,,*47\r\n");

        GpsData reading = device.Read();

        Assert.Equal(-48.1173, reading.Latitude, 4);
        Assert.Equal(-11.5166667, reading.Longitude, 4);
    }

    [Fact]
    public void Read_BeforeInitialize_Throws()
    {
        var transport = new FakeSerialTransport();
        using var device = new NmeaGpsDevice("gps-01", transport);

        Assert.Throws<InvalidOperationException>(() => device.Read());
    }

    [Fact]
    public void Initialize_Twice_Throws()
    {
        var (device, _) = CreateDevice();

        Assert.Throws<InvalidOperationException>(() => device.Initialize());
    }

    [Fact]
    public void Reset_ClearsReadingAndDisposesTransport()
    {
        var (device, transport) = CreateDevice();
        transport.Feed(GgaSentence);
        device.Read();

        device.Reset();

        Assert.Equal(GpsData.Empty, device.LatestReading);
        Assert.False(device.IsInitialized);
        Assert.True(transport.Disposed);
    }

    private static string GgaLine(int fixQuality) =>
        FormattableString.Invariant(
            $"$GPGGA,123519,4807.038,N,01131.000,E,{fixQuality},08,0.9,545.4,M,46.9,M,,\r\n");

    private static string RmcLine(double courseDegrees) =>
        FormattableString.Invariant(
            $"$GPRMC,123519,A,4807.038,N,01131.000,E,022.4,{courseDegrees:000.0},230394,003.1,W\r\n");

    private static (NmeaGpsDevice Device, FakeSerialTransport Transport) CreateDevice()
    {
        var transport = new FakeSerialTransport();
        var device = new NmeaGpsDevice("gps-01", transport);
        device.Initialize();
        return (device, transport);
    }
}
