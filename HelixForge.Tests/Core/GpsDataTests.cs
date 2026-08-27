using HelixForge;

namespace HelixForge.Tests.Core;

public class GpsDataTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var data = new GpsData(37.7749, -122.4194, 10.5, new Vector3(1, 2, 3), TimeSpan.FromSeconds(2));

        Assert.Equal(37.7749, data.Latitude);
        Assert.Equal(-122.4194, data.Longitude);
        Assert.Equal(10.5, data.Altitude);
        Assert.Equal(new Vector3(1, 2, 3), data.Velocity);
        Assert.Equal(TimeSpan.FromSeconds(2), data.Timestamp);
        Assert.Equal(GpsFixStatus.Fix3D, data.FixStatus); // default
    }

    [Fact]
    public void Constructor_WithFixStatus_SetsFixStatus()
    {
        var data = new GpsData(37.7749, -122.4194, 10.5, Vector3.Zero, TimeSpan.FromSeconds(1), GpsFixStatus.NoFix);
        Assert.Equal(GpsFixStatus.NoFix, data.FixStatus);
    }

    [Fact]
    public void Empty_HasZeroValues()
    {
        var data = GpsData.Empty;
        Assert.Equal(0.0, data.Latitude);
        Assert.Equal(0.0, data.Longitude);
        Assert.Equal(0.0, data.Altitude);
        Assert.Equal(Vector3.Zero, data.Velocity);
        Assert.Equal(TimeSpan.Zero, data.Timestamp);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new GpsData(37.7749, -122.4194, 10.5, Vector3.Zero, TimeSpan.FromSeconds(1));
        var b = new GpsData(37.7749, -122.4194, 10.5, Vector3.Zero, TimeSpan.FromSeconds(1));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new GpsData(37.7749, -122.4194, 10.5, Vector3.Zero, TimeSpan.FromSeconds(1));
        var b = new GpsData(37.7749, -122.4194, 20.0, Vector3.Zero, TimeSpan.FromSeconds(1)); // Different altitude
        Assert.NotEqual(a, b);
    }
}
