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

    [Fact]
    public void Battery_BatteryData_TracksExpectation()
    {
        var config = new BatterySimConfig
        {
            FullVoltage = 12.6,
            EmptyVoltage = 9.0,
            InternalResistance = 0.1,
            CapacityAh = 1.0,
            InitialCharge = 1.0
        };
        var battery = new SimBatteryDevice("batt-01", config, new ICurrentConsumer[] { new ConstConsumer(2.0) });
        battery.Initialize();
        battery.Update(TimeSpan.FromSeconds(1800)); // 0.5h at 2A = 1.0Ah of 1.0Ah -> empty

        var data = battery.Read();
        Assert.Equal(0.0, data.ChargeFraction, 6);
    }

    [Fact]
    public void Environment_Gps_SameSeed_ProducesIdenticalReadings()
    {
        var r1 = RunEnvGpsSimulation(seed: 5);
        var r2 = RunEnvGpsSimulation(seed: 5);

        Assert.Equal(r1.Count, r2.Count);
        for (int i = 0; i < r1.Count; i++)
            Assert.Equal(r1[i], r2[i]);
    }

    [Fact]
    public void DifferentialDrive_Deterministic_EqualSpeeds()
    {
        var config = new DifferentialDriveSimConfig { OdometryNoise = 0.0 };
        var drive = new SimDifferentialDriveDevice("dd-01", config);
        drive.Initialize();
        drive.SetTargetSpeeds(1.0, 1.0);
        drive.Update(TimeSpan.FromSeconds(1));

        var data = drive.Read();
        Assert.Equal(1.0, data.Pose.X, 6);
        Assert.Equal(0.0, data.Pose.Y, 6);
    }

    private List<GpsData> RunEnvGpsSimulation(int seed)
    {
        var env = new EnvironmentSimConfig
        {
            WindVelocity = new Vector3(2.0, -1.0, 0.0),
            WindGustNoise = 0.5,
            Turbulence = 1.0,
            ReferenceAltitudeMeters = 100.0,
            RandomSeed = seed
        };
        var gpsConfig = new GpsSimConfig
        {
            PositionNoise = 0.0,
            RandomSeed = seed,
            Environment = env
        };
        var gps = new SimGpsDevice("gps-01", gpsConfig);
        gps.Initialize();

        var results = new List<GpsData>();
        for (int i = 0; i < 100; i++)
        {
            gps.Update(TimeSpan.FromMilliseconds(10));
            results.Add(gps.Read());
        }

        return results;
    }

    private sealed class ConstConsumer : ICurrentConsumer
    {
        private readonly double _c;
        public ConstConsumer(double c) => _c = c;
        public double Current => _c;
    }
}
