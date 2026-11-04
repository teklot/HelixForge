using System;
using System.Buffers.Binary;
using HelixForge.Hardware.Drivers;
using HelixForge.Hardware.Transports;

namespace HelixForge.Tests.Hardware;

public class UbxGpsDeviceTests
{
    private const int NavPvtPayloadLength = 92;

    [Fact]
    public void Read_ParsesNavPvtPositionAltitudeAndVelocity()
    {
        var (device, transport) = CreateDevice();
        transport.Feed(BuildNavPvt(
            latitude: 377749000,
            longitude: -1224194000,
            heightMsl: 10500,
            velocityNorth: 1000,
            velocityEast: 2000,
            velocityDown: 3000));

        GpsData reading = device.Read();

        Assert.Equal(37.7749, reading.Latitude, 6);
        Assert.Equal(-122.4194, reading.Longitude, 6);
        Assert.Equal(10.5, reading.Altitude, 3);
        Assert.Equal(1.0, reading.Velocity.X, 3);
        Assert.Equal(2.0, reading.Velocity.Y, 3);
        Assert.Equal(-3.0, reading.Velocity.Z, 3);
    }

    [Theory]
    [InlineData(0, GpsFixStatus.NoFix)]
    [InlineData(1, GpsFixStatus.NoFix)]
    [InlineData(2, GpsFixStatus.Fix2D)]
    [InlineData(3, GpsFixStatus.Fix3D)]
    [InlineData(4, GpsFixStatus.Fix3D)]
    [InlineData(5, GpsFixStatus.NoFix)]
    public void Read_MapsFixTypeToFixStatus(byte fixType, GpsFixStatus expected)
    {
        var (device, transport) = CreateDevice();
        transport.Feed(BuildNavPvt(fixType: fixType));

        GpsData reading = device.Read();

        Assert.Equal(expected, reading.FixStatus);
    }

    [Fact]
    public void Read_IgnoresFrameWithInvalidChecksum()
    {
        var (device, transport) = CreateDevice();
        transport.Feed(BuildNavPvt(latitude: 377749000, corruptChecksum: true));

        GpsData reading = device.Read();

        Assert.Equal(GpsData.Empty, reading);
    }

    [Fact]
    public void Read_HandlesFrameSplitAcrossReads()
    {
        var (device, transport) = CreateDevice();
        byte[] frame = BuildNavPvt(latitude: 377749000);

        transport.Feed(frame.AsSpan(0, 40).ToArray());
        GpsData partial = device.Read();
        Assert.Equal(GpsData.Empty, partial);

        transport.Feed(frame.AsSpan(40).ToArray());
        GpsData complete = device.Read();

        Assert.Equal(37.7749, complete.Latitude, 6);
    }

    [Fact]
    public void Read_SkipsGarbageBeforeFrame()
    {
        var (device, transport) = CreateDevice();
        byte[] frame = BuildNavPvt(latitude: 377749000);
        var payload = new byte[] { 0x00, 0xB5, 0x11, 0xB5, 0x62, 0x00, 0xFF, 0xEE };

        transport.Feed(payload);
        transport.Feed(frame);

        GpsData reading = device.Read();

        Assert.Equal(37.7749, reading.Latitude, 6);
    }

    [Fact]
    public void Read_IgnoresOtherUbxMessages()
    {
        var (device, transport) = CreateDevice();
        var payload = new byte[NavPvtPayloadLength];
        transport.Feed(BuildUbxFrame(messageId: 0x05, payload));

        GpsData reading = device.Read();

        Assert.Equal(GpsData.Empty, reading);
    }

    [Fact]
    public void Read_BeforeInitialize_Throws()
    {
        var transport = new FakeSerialTransport();
        using var device = new UbxGpsDevice("gps-01", transport);

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
        transport.Feed(BuildNavPvt(latitude: 377749000));
        device.Read();

        device.Reset();

        Assert.Equal(GpsData.Empty, device.LatestReading);
        Assert.False(device.IsInitialized);
        Assert.True(transport.Disposed);
    }

    private static (UbxGpsDevice Device, FakeSerialTransport Transport) CreateDevice()
    {
        var transport = new FakeSerialTransport();
        var device = new UbxGpsDevice("gps-01", transport);
        device.Initialize();
        return (device, transport);
    }

    private static byte[] BuildNavPvt(
        byte fixType = 3,
        int latitude = 0,
        int longitude = 0,
        int heightMsl = 0,
        int velocityNorth = 0,
        int velocityEast = 0,
        int velocityDown = 0,
        bool corruptChecksum = false)
    {
        var payload = new byte[NavPvtPayloadLength];
        payload[20] = fixType;
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(24), longitude);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(28), latitude);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(36), heightMsl);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(48), velocityNorth);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(52), velocityEast);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(56), velocityDown);

        byte[] frame = BuildUbxFrame(messageId: 0x07, payload);

        if (corruptChecksum)
            frame[frame.Length - 1] ^= 0xFF;

        return frame;
    }

    private static byte[] BuildUbxFrame(byte messageId, byte[] payload)
    {
        var frame = new byte[payload.Length + 8];
        frame[0] = 0xB5;
        frame[1] = 0x62;
        frame[2] = 0x01;
        frame[3] = messageId;
        frame[4] = (byte)(payload.Length & 0xFF);
        frame[5] = (byte)(payload.Length >> 8);
        Array.Copy(payload, 0, frame, 6, payload.Length);

        byte checksumA = 0;
        byte checksumB = 0;
        for (int i = 2; i < 6 + payload.Length; i++)
        {
            checksumA += frame[i];
            checksumB += checksumA;
        }

        frame[frame.Length - 2] = checksumA;
        frame[frame.Length - 1] = checksumB;
        return frame;
    }
}
