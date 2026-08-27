using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class SimMagDeviceTests
{
    [Fact]
    public void Initialize_SetsIsInitialized()
    {
        var mag = new SimMagDevice("mag-01", new MagSimConfig());
        Assert.False(mag.IsInitialized);

        mag.Initialize();

        Assert.True(mag.IsInitialized);
    }

    [Fact]
    public void Initialize_CalledTwice_Throws()
    {
        var mag = new SimMagDevice("mag-01", new MagSimConfig());
        mag.Initialize();

        Assert.Throws<InvalidOperationException>(() => mag.Initialize());
    }

    [Fact]
    public void Read_BeforeInitialize_ReturnsEmpty()
    {
        var mag = new SimMagDevice("mag-01", new MagSimConfig());
        var data = mag.Read();
        Assert.Equal(MagData.Empty, data);
    }

    [Fact]
    public void Update_AdvancesTime()
    {
        var mag = new SimMagDevice("mag-01", new MagSimConfig());
        mag.Initialize();

        mag.Update(TimeSpan.FromSeconds(1));

        var data = mag.Read();
        Assert.Equal(TimeSpan.FromSeconds(1), data.Timestamp);
    }

    [Fact]
    public void Reading_AppliesHardIronOffset()
    {
        var config = new MagSimConfig
        {
            EarthField = new Vector3(25, 0, -45),
            HardIronOffset = new Vector3(5, 0, 0),
            MagneticNoise = 0.0,
            RandomSeed = 42
        };
        var mag = new SimMagDevice("mag-01", config);
        mag.Initialize();

        mag.Update(TimeSpan.FromSeconds(1));

        var data = mag.Read();
        Assert.Equal(30.0, data.MagneticField.X, 4);
    }

    [Fact]
    public void Readings_AppliesDeclination()
    {
        var config = new MagSimConfig
        {
            EarthField = new Vector3(25, 0, 0),
            DeclinationDeg = 90.0,
            MagneticNoise = 0.0,
            RandomSeed = 42
        };
        var mag = new SimMagDevice("mag-01", config);
        mag.Initialize();

        mag.SetOrientation(Vector3.Zero);
        mag.Update(TimeSpan.FromSeconds(1));

        var data = mag.Read();
        // 90-degree declination rotates the horizontal field: X -> 0, Y -> 25
        Assert.Equal(0.0, data.MagneticField.X, 4);
        Assert.Equal(25.0, data.MagneticField.Y, 4);
    }

    [Fact]
    public void SetOrientation_RotatesBodyField()
    {
        var config = new MagSimConfig
        {
            EarthField = new Vector3(25, 0, 0),
            MagneticNoise = 0.0,
            RandomSeed = 42
        };
        var mag = new SimMagDevice("mag-01", config);
        mag.Initialize();

        mag.SetOrientation(new Vector3(0, 0, Math.PI / 2)); // 90 deg yaw
        mag.Update(TimeSpan.FromSeconds(1));

        var data = mag.Read();
        Assert.Equal(0.0, data.MagneticField.X, 4);
        Assert.Equal(-25.0, data.MagneticField.Y, 4);
    }

    [Fact]
    public void Reading_AppliesNoise()
    {
        var config = new MagSimConfig
        {
            EarthField = new Vector3(25, 0, 0),
            MagneticNoise = 1.0,
            RandomSeed = 42
        };
        var mag = new SimMagDevice("mag-01", config);
        mag.Initialize();

        mag.Update(TimeSpan.FromSeconds(1));

        var data = mag.Read();
        Assert.NotEqual(25.0, data.MagneticField.X, 4);
    }

    [Fact]
    public void Reset_ReturnsToInitialState()
    {
        var config = new MagSimConfig { RandomSeed = 42 };
        var mag = new SimMagDevice("mag-01", config);
        mag.Initialize();

        mag.SetOrientation(new Vector3(1, 0, 0));
        mag.Update(TimeSpan.FromSeconds(1));
        mag.Reset();

        var data = mag.Read();
        Assert.Equal(TimeSpan.Zero, data.Timestamp);
    }

    [Fact]
    public void DeviceId_ReturnsCorrectId()
    {
        var mag = new SimMagDevice("my-mag", new MagSimConfig());
        Assert.Equal("my-mag", mag.DeviceId);
    }
}
