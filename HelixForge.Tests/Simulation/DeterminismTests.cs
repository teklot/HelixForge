using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class DeterminismTests
{
    [Fact]
    public void SameSeed_ProducesSameResults()
    {
        var results1 = RunSimulation(seed: 42);
        var results2 = RunSimulation(seed: 42);

        Assert.Equal(results1.Count, results2.Count);
        for (int i = 0; i < results1.Count; i++)
        {
            Assert.Equal(results1[i].Orientation.X, results2[i].Orientation.X, 10);
            Assert.Equal(results1[i].Orientation.Y, results2[i].Orientation.Y, 10);
            Assert.Equal(results1[i].Orientation.Z, results2[i].Orientation.Z, 10);
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentResults()
    {
        var results1 = RunSimulation(seed: 42);
        var results2 = RunSimulation(seed: 9999);

        bool anyDifferent = false;
        for (int i = 1; i < Math.Min(results1.Count, results2.Count); i++)
        {
            if (results1[i].Acceleration.X != results2[i].Acceleration.X)
            {
                anyDifferent = true;
                break;
            }
        }

        Assert.True(anyDifferent, "Different seeds should produce different noisy acceleration readings");
    }

    [Fact]
    public void Gps_SameSeed_ProducesIdenticalTrajectory()
    {
        var results1 = RunGpsSimulation(seed: 123);
        var results2 = RunGpsSimulation(seed: 123);

        Assert.Equal(results1.Count, results2.Count);
        for (int i = 0; i < results1.Count; i++)
        {
            Assert.Equal(results1[i], results2[i]);
        }
    }

    [Fact]
    public void Gps_DifferentSeeds_ProduceDifferentResults()
    {
        var results1 = RunGpsSimulation(seed: 123);
        var results2 = RunGpsSimulation(seed: 456);

        bool anyDifferent = false;
        for (int i = 0; i < Math.Min(results1.Count, results2.Count); i++)
        {
            if (results1[i].Latitude != results2[i].Latitude)
            {
                anyDifferent = true;
                break;
            }
        }

        Assert.True(anyDifferent, "Different seeds should produce different noisy position readings");
    }

    private List<ImuData> RunSimulation(int seed)
    {
        var registry = new DeviceRegistry();
        var imuConfig = new ImuSimConfig
        {
            AccelerometerNoise = 1.0,
            GyroscopeNoise = 1.0,
            RandomSeed = seed
        };
        var imu = new SimImuDevice("imu-01", imuConfig);
        imu.Initialize();
        registry.Register(imu);

        var config = new SimulationConfig
        {
            TimeStep = TimeSpan.FromMilliseconds(10)
        };

        var engine = new SimulationEngine(registry, null, config);
        var results = new List<ImuData>();

        engine.Run(TimeSpan.FromSeconds(1), _ =>
        {
            results.Add(imu.Read());
        });

        return results;
    }

    private List<GpsData> RunGpsSimulation(int seed)
    {
        var gpsConfig = new GpsSimConfig
        {
            PositionNoise = 2.0,
            RandomSeed = seed
        };
        var gps = new SimGpsDevice("gps-01", gpsConfig);
        gps.Initialize();
        gps.SetVelocity(new Vector3(10, 0, 0));

        var results = new List<GpsData>();
        for (int i = 0; i < 100; i++)
        {
            gps.Update(TimeSpan.FromMilliseconds(10));
            results.Add(gps.Read());
        }

        return results;
    }

    [Fact]
    public void Mag_SameSeed_ProducesIdenticalReadings()
    {
        var results1 = RunMagSimulation(seed: 11);
        var results2 = RunMagSimulation(seed: 11);

        Assert.Equal(results1.Count, results2.Count);
        for (int i = 0; i < results1.Count; i++)
        {
            Assert.Equal(results1[i], results2[i]);
        }
    }

    [Fact]
    public void Baro_SameSeed_ProducesIdenticalReadings()
    {
        var results1 = RunBaroSimulation(seed: 22);
        var results2 = RunBaroSimulation(seed: 22);

        Assert.Equal(results1.Count, results2.Count);
        for (int i = 0; i < results1.Count; i++)
        {
            Assert.Equal(results1[i], results2[i]);
        }
    }

    private List<MagData> RunMagSimulation(int seed)
    {
        var config = new MagSimConfig { MagneticNoise = 1.0, RandomSeed = seed };
        var mag = new SimMagDevice("mag-01", config);
        mag.Initialize();

        var results = new List<MagData>();
        for (int i = 0; i < 100; i++)
        {
            mag.Update(TimeSpan.FromMilliseconds(10));
            results.Add(mag.Read());
        }

        return results;
    }

    private List<BarometerData> RunBaroSimulation(int seed)
    {
        var config = new BarometerSimConfig { AltitudeNoise = 1.0, RandomSeed = seed };
        var baro = new SimBarometerDevice("baro-01", config);
        baro.Initialize();

        var results = new List<BarometerData>();
        for (int i = 0; i < 100; i++)
        {
            baro.Update(TimeSpan.FromMilliseconds(10));
            results.Add(baro.Read());
        }

        return results;
    }
}
