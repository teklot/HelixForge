using System;
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

    [Fact]
    public void Read_ParsesRmcSpeedAsMetersPerSecond()
    {
        var (device, transport) = CreateDevice();
        transport.Feed(GgaSentence);
        transport.Feed(RmcSentence);

        GpsData reading = device.Read();

        Assert.Equal(22.4 * 0.514444, reading.Velocity.X, 4);
        Assert.Equal(0.0, reading.Velocity.Y, 3);
        Assert.Equal(0.0, reading.Velocity.Z, 3);
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

    private static (NmeaGpsDevice Device, FakeSerialTransport Transport) CreateDevice()
    {
        var transport = new FakeSerialTransport();
        var device = new NmeaGpsDevice("gps-01", transport);
        device.Initialize();
        return (device, transport);
    }
}
