using HelixForge;

namespace HelixForge.Tests.Core;

public class ImuDataTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var orient = new Vector3(0.1, 0.2, 0.3);
        var angVel = new Vector3(1.0, 2.0, 3.0);
        var accel = new Vector3(9.8, 0.0, 0.0);
        var ts = TimeSpan.FromSeconds(1.5);

        var data = new ImuData(orient, angVel, accel, ts);

        Assert.Equal(orient, data.Orientation);
        Assert.Equal(angVel, data.AngularVelocity);
        Assert.Equal(accel, data.Acceleration);
        Assert.Equal(ts, data.Timestamp);
    }

    [Fact]
    public void Empty_HasZeroValues()
    {
        var data = ImuData.Empty;
        Assert.Equal(Vector3.Zero, data.Orientation);
        Assert.Equal(Vector3.Zero, data.AngularVelocity);
        Assert.Equal(Vector3.Zero, data.Acceleration);
        Assert.Equal(TimeSpan.Zero, data.Timestamp);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new ImuData(
            new Vector3(1, 2, 3),
            new Vector3(4, 5, 6),
            new Vector3(7, 8, 9),
            TimeSpan.FromSeconds(1));

        var b = new ImuData(
            new Vector3(1, 2, 3),
            new Vector3(4, 5, 6),
            new Vector3(7, 8, 9),
            TimeSpan.FromSeconds(1));

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new ImuData(
            new Vector3(1, 2, 3),
            new Vector3(4, 5, 6),
            new Vector3(7, 8, 9),
            TimeSpan.FromSeconds(1));

        var b = new ImuData(
            new Vector3(1, 2, 3),
            new Vector3(4, 5, 6),
            new Vector3(7, 8, 10), // Different
            TimeSpan.FromSeconds(1));

        Assert.NotEqual(a, b);
    }
}
