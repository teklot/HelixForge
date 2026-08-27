using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class SimGpsDeviceDropoutTests
{
    [Fact]
    public void DefaultConfig_ReportsBaseFixStatus()
    {
        var config = new GpsSimConfig { RandomSeed = 42 };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        gps.Update(TimeSpan.FromSeconds(1));

        var data = gps.Read();
        Assert.Equal(GpsFixStatus.Fix3D, data.FixStatus);
    }

    [Fact]
    public void CustomBaseFixStatus_IsReported()
    {
        var config = new GpsSimConfig
        {
            BaseFixStatus = GpsFixStatus.Fix2D,
            RandomSeed = 42
        };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        var data = gps.Read();

        Assert.Equal(GpsFixStatus.Fix2D, data.FixStatus);
    }

    [Fact]
    public void HighDropoutRate_ReportsNoFix()
    {
        var config = new GpsSimConfig
        {
            DropoutRate = 1000.0, // essentially always in dropout
            MaxDropoutDuration = 5.0,
            RandomSeed = 42
        };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        gps.SetVelocity(new Vector3(10, 0, 0));
        gps.Update(TimeSpan.FromSeconds(0.1));

        var data = gps.Read();
        Assert.Equal(GpsFixStatus.NoFix, data.FixStatus);
    }

    [Fact]
    public void Dropout_IncreasesUncertainty()
    {
        var config = new GpsSimConfig
        {
            PositionNoise = 0.0,
            DropoutRate = 1000.0,
            MaxDropoutDuration = 5.0,
            UncertaintyDuringDropout = 100000.0, // huge uncertainty so a single sample is very large
            RandomSeed = 42
        };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        gps.SetVelocity(new Vector3(0, 0, 0)); // no motion
        gps.Update(TimeSpan.FromSeconds(0.1)); // enter dropout

        var data = gps.Read();
        Assert.Equal(GpsFixStatus.NoFix, data.FixStatus);
        // During dropout, positional deviation is large (100km uncertainty), far beyond noise-free zero.
        Assert.True(Math.Abs(data.Latitude) > 0.01, $"Expected large positional deviation during dropout, got {data.Latitude}");
    }

    [Fact]
    public void NoDropout_PositionTrackVelocity()
    {
        var config = new GpsSimConfig
        {
            PositionNoise = 0.0,
            DropoutRate = 0.0,
            RandomSeed = 42
        };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        gps.SetVelocity(new Vector3(10, 0, 0)); // North at 10 m/s
        gps.Update(TimeSpan.FromSeconds(1));

        var data = gps.Read();
        Assert.Equal(GpsFixStatus.Fix3D, data.FixStatus);
        // 10 m/s for 1s northward = ~0.00009 deg latitude
        Assert.True(data.Latitude > 0.00008 && data.Latitude < 0.0001);
    }

    [Fact]
    public void Deterministic_SameDropoutSchedule_IsRepeatable()
    {
        var results1 = RunDropoutSimulation(seed: 7);
        var results2 = RunDropoutSimulation(seed: 7);

        Assert.Equal(results1.Count, results2.Count);
        for (int i = 0; i < results1.Count; i++)
        {
            Assert.Equal(results1[i], results2[i]);
        }
    }

    private static List<GpsData> RunDropoutSimulation(int seed)
    {
        var config = new GpsSimConfig
        {
            DropoutRate = 2.0,
            MaxDropoutDuration = 0.5,
            RandomSeed = seed
        };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        var results = new List<GpsData>();
        for (int i = 0; i < 500; i++)
        {
            gps.Update(TimeSpan.FromMilliseconds(10));
            results.Add(gps.Read());
        }

        return results;
    }
}
