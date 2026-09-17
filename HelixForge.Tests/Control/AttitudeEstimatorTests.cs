using HelixForge;
using HelixForge.Control;

namespace HelixForge.Tests.Control;

public class QuaternionTests
{
    [Fact]
    public void Identity_NoRotation()
    {
        var v = new Vector3(1, 2, 3);
        var rotated = Quaternion.Identity.Rotate(v);

        Assert.Equal(v.X, rotated.X, 10);
        Assert.Equal(v.Y, rotated.Y, 10);
        Assert.Equal(v.Z, rotated.Z, 10);
    }

    [Fact]
    public void FromEuler_ToEuler_RoundTrips()
    {
        var euler = new Vector3(0.3, -0.2, 0.7);

        var q = Quaternion.FromEuler(euler);
        var back = q.ToEuler();

        Assert.Equal(euler.X, back.X, 6);
        Assert.Equal(euler.Y, back.Y, 6);
        Assert.Equal(euler.Z, back.Z, 6);
    }

    [Fact]
    public void Rotate_90DegreesAboutZ_MapsXToY()
    {
        var q = Quaternion.FromEuler(0, 0, Math.PI / 2.0);

        var rotated = q.Rotate(new Vector3(1, 0, 0));

        Assert.Equal(0.0, rotated.X, 8);
        Assert.Equal(1.0, rotated.Y, 8);
        Assert.Equal(0.0, rotated.Z, 8);
    }

    [Fact]
    public void Normalized_IsUnitLength()
    {
        var q = new Quaternion(2.0, 3.0, -1.0, 0.5).Normalized;

        double lengthSquared = q.W * q.W + q.X * q.X + q.Y * q.Y + q.Z * q.Z;
        Assert.Equal(1.0, lengthSquared, 10);
    }
}

public class ComplementaryFilterTests
{
    [Fact]
    public void Stationary_Level_ReadsLevel()
    {
        var filter = new ComplementaryFilter(new ComplementaryFilterConfig { Alpha = 0.98 });

        for (int i = 0; i < 100; i++)
            filter.Update(TimeSpan.FromMilliseconds(10), new Vector3(0, 0, 0), new Vector3(0, 0, 9.81));

        var orientation = filter.Orientation;
        Assert.Equal(0.0, orientation.X, 4);
        Assert.Equal(0.0, orientation.Y, 4);
    }

    [Fact]
    public void StartsTilted_ConvergesToLevel()
    {
        var filter = new ComplementaryFilter(new ComplementaryFilterConfig
        {
            Alpha = 0.1,
            InitialOrientation = new Vector3(0.5, -0.3, 0.0)
        });

        for (int i = 0; i < 300; i++)
            filter.Update(TimeSpan.FromMilliseconds(10), new Vector3(0, 0, 0), new Vector3(0, 0, 9.81));

        var orientation = filter.Orientation;
        Assert.Equal(0.0, orientation.X, 2);
        Assert.Equal(0.0, orientation.Y, 2);
    }

    [Fact]
    public void PureGyro_IntegratesYaw()
    {
        var filter = new ComplementaryFilter(new ComplementaryFilterConfig
        {
            Alpha = 1.0,
            InitialOrientation = new Vector3(0, 0, 0)
        });

        double dt = 0.01;
        for (int i = 0; i < 100; i++)
            filter.Update(TimeSpan.FromSeconds(dt), new Vector3(0, 0, 1.0), new Vector3(0, 0, 9.81));

        Assert.Equal(1.0, filter.Orientation.Z, 4); // 100 * 0.01s * 1 rad/s = 1 rad
    }

    [Fact]
    public void Magnetometer_AlignsYaw()
    {
        var filter = new ComplementaryFilter(new ComplementaryFilterConfig
        {
            Alpha = 0.1,
            InitialOrientation = new Vector3(0, 0, 0.8)
        });

        var mag = new Vector3(20.0, 0.0, -45.0); // magnetic north roughly along +X

        for (int i = 0; i < 300; i++)
            filter.Update(TimeSpan.FromMilliseconds(10), new Vector3(0, 0, 0), new Vector3(0, 0, 9.81), mag);

        Assert.Equal(0.0, filter.Orientation.Z, 1);
    }
}

public class MadgwickFilterTests
{
    [Fact]
    public void Stationary_Level_ReadsLevel()
    {
        var filter = new MadgwickFilter(new MadgwickFilterConfig { Beta = 0.5 });

        for (int i = 0; i < 100; i++)
            filter.Update(TimeSpan.FromMilliseconds(10), new Vector3(0, 0, 0), new Vector3(0, 0, 9.81));

        var orientation = filter.Orientation;
        Assert.Equal(0.0, orientation.X, 4);
        Assert.Equal(0.0, orientation.Y, 4);
    }

    [Fact]
    public void StartsTilted_ConvergesToLevel()
    {
        var filter = new MadgwickFilter(new MadgwickFilterConfig
        {
            Beta = 0.5,
            InitialQuaternion = Quaternion.FromEuler(0.6, -0.4, 0.0)
        });

        for (int i = 0; i < 600; i++)
            filter.Update(TimeSpan.FromMilliseconds(10), new Vector3(0, 0, 0), new Vector3(0, 0, 9.81));

        var orientation = filter.Orientation;
        Assert.Equal(0.0, orientation.X, 2);
        Assert.Equal(0.0, orientation.Y, 2);
    }

    [Fact]
    public void Magnetometer_AlignsYaw()
    {
        var filter = new MadgwickFilter(new MadgwickFilterConfig
        {
            Beta = 0.5,
            InitialQuaternion = Quaternion.FromEuler(0.0, 0.0, 0.8)
        });

        var mag = new Vector3(20.0, 0.0, -45.0);

        for (int i = 0; i < 600; i++)
            filter.Update(TimeSpan.FromMilliseconds(10), new Vector3(0, 0, 0), new Vector3(0, 0, 9.81), mag);

        Assert.Equal(0.0, filter.Orientation.Z, 1);
    }

    [Fact]
    public void InitialQuaternion_IsUsed()
    {
        var filter = new MadgwickFilter(new MadgwickFilterConfig
        {
            InitialQuaternion = Quaternion.FromEuler(0.3, 0.0, 0.0)
        });

        Assert.Equal(0.3, filter.Orientation.X, 6);
    }
}