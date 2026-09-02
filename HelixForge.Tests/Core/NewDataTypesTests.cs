using HelixForge;

namespace HelixForge.Tests.Core;

public class BatteryDataTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var data = new BatteryData(11.6, 10.0, 0.9, TimeSpan.FromSeconds(2));

        Assert.Equal(11.6, data.Voltage);
        Assert.Equal(10.0, data.Current);
        Assert.Equal(0.9, data.ChargeFraction);
        Assert.Equal(TimeSpan.FromSeconds(2), data.Timestamp);
    }

    [Fact]
    public void Empty_HasZeroValues()
    {
        var data = BatteryData.Empty;
        Assert.Equal(0.0, data.Voltage);
        Assert.Equal(0.0, data.Current);
        Assert.Equal(0.0, data.ChargeFraction);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new BatteryData(11.6, 10.0, 0.9, TimeSpan.FromSeconds(2));
        var b = new BatteryData(11.6, 10.0, 0.9, TimeSpan.FromSeconds(2));

        Assert.True(a.Equals(b));
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new BatteryData(11.6, 10.0, 0.9, TimeSpan.FromSeconds(2));
        var b = new BatteryData(12.0, 10.0, 0.9, TimeSpan.FromSeconds(2));

        Assert.False(a.Equals(b));
    }
}

public class Pose2DTests
{
    [Fact]
    public void Zero_IsOrigin()
    {
        var pose = Pose2D.Zero;
        Assert.Equal(0.0, pose.X);
        Assert.Equal(0.0, pose.Y);
        Assert.Equal(0.0, pose.Yaw);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new Pose2D(1.0, 2.0, 0.5);
        var b = new Pose2D(1.0, 2.0, 0.5);

        Assert.True(a.Equals(b));
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentYaw_AreNotEqual()
    {
        var a = new Pose2D(1.0, 2.0, 0.5);
        var b = new Pose2D(1.0, 2.0, 0.7);

        Assert.False(a.Equals(b));
    }
}

public class DifferentialDriveDataTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var pose = new Pose2D(1, 2, 0.5);
        var data = new DifferentialDriveData(pose, 0.5, 2.0, TimeSpan.FromSeconds(1));

        Assert.Equal(pose, data.Pose);
        Assert.Equal(0.5, data.LinearSpeed);
        Assert.Equal(2.0, data.AngularSpeed);
        Assert.Equal(TimeSpan.FromSeconds(1), data.Timestamp);
    }

    [Fact]
    public void Empty_HasZeroValues()
    {
        var data = DifferentialDriveData.Empty;
        Assert.Equal(Pose2D.Zero, data.Pose);
        Assert.Equal(0.0, data.LinearSpeed);
        Assert.Equal(0.0, data.AngularSpeed);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new DifferentialDriveData(new Pose2D(1, 2, 0.5), 0.5, 2.0, TimeSpan.FromSeconds(1));
        var b = new DifferentialDriveData(new Pose2D(1, 2, 0.5), 0.5, 2.0, TimeSpan.FromSeconds(1));

        Assert.True(a.Equals(b));
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
