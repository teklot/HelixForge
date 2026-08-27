using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class SimBarometerDeviceTests
{
    [Fact]
    public void Initialize_SetsIsInitialized()
    {
        var baro = new SimBarometerDevice("baro-01", new BarometerSimConfig());
        Assert.False(baro.IsInitialized);

        baro.Initialize();

        Assert.True(baro.IsInitialized);
    }

    [Fact]
    public void Initialize_CalledTwice_Throws()
    {
        var baro = new SimBarometerDevice("baro-01", new BarometerSimConfig());
        baro.Initialize();

        Assert.Throws<InvalidOperationException>(() => baro.Initialize());
    }

    [Fact]
    public void Read_BeforeInitialize_ReturnsEmpty()
    {
        var baro = new SimBarometerDevice("baro-01", new BarometerSimConfig());
        var data = baro.Read();
        Assert.Equal(BarometerData.Empty, data);
    }

    [Fact]
    public void Update_AdvancesTime()
    {
        var baro = new SimBarometerDevice("baro-01", new BarometerSimConfig());
        baro.Initialize();

        baro.Update(TimeSpan.FromSeconds(1));

        var data = baro.Read();
        Assert.Equal(TimeSpan.FromSeconds(1), data.Timestamp);
    }

    [Fact]
    public void SeaLevelAltitude_ReportsSeaLevelPressure()
    {
        var config = new BarometerSimConfig
        {
            SeaLevelPressure = 1013.25,
            AltitudeNoise = 0.0,
            InitialAltitude = 0.0,
            RandomSeed = 42
        };
        var baro = new SimBarometerDevice("baro-01", config);
        baro.Initialize();

        var data = baro.Read();

        Assert.Equal(1013.25, data.Pressure, 4);
        Assert.Equal(0.0, data.Altitude, 4);
    }

    [Fact]
    public void ElevatedAltitude_ReducesPressure()
    {
        var config = new BarometerSimConfig
        {
            SeaLevelPressure = 1013.25,
            AltitudeNoise = 0.0,
            InitialAltitude = 1000.0,
            RandomSeed = 42
        };
        var baro = new SimBarometerDevice("baro-01", config);
        baro.Initialize();

        var data = baro.Read();

        Assert.True(data.Pressure < 1013.25);
        Assert.Equal(1000.0, data.Altitude, 1);
    }

    [Fact]
    public void SetAltitude_DerivesPressureFromBarometricFormula()
    {
        var config = new BarometerSimConfig
        {
            SeaLevelPressure = 1013.25,
            AltitudeNoise = 0.0,
            RandomSeed = 42
        };
        var baro = new SimBarometerDevice("baro-01", config);
        baro.Initialize();

        baro.SetAltitude(500.0);
        baro.Update(TimeSpan.FromSeconds(1));

        var data = baro.Read();
        // 500m -> roughly 954-956 hPa; just assert it's below sea-level pressure
        Assert.True(data.Pressure < 1013.25);
        Assert.InRange(data.Pressure, 940, 990);
    }

    [Fact]
    public void Update_AppliesNoise()
    {
        var config = new BarometerSimConfig
        {
            AltitudeNoise = 1.0,
            RandomSeed = 42
        };
        var baro = new SimBarometerDevice("baro-01", config);
        baro.Initialize();

        baro.Update(TimeSpan.FromSeconds(1));

        var data = baro.Read();
        Assert.NotEqual(0.0, data.Altitude, 4);
    }

    [Fact]
    public void Reset_ReturnsToInitialState()
    {
        var config = new BarometerSimConfig { InitialAltitude = 200.0, RandomSeed = 42 };
        var baro = new SimBarometerDevice("baro-01", config);
        baro.Initialize();

        baro.SetAltitude(500.0);
        baro.Update(TimeSpan.FromSeconds(1));
        baro.Reset();

        var data = baro.Read();
        Assert.Equal(200.0, data.Altitude, 4);
    }

    [Fact]
    public void DeviceId_ReturnsCorrectId()
    {
        var baro = new SimBarometerDevice("my-baro", new BarometerSimConfig());
        Assert.Equal("my-baro", baro.DeviceId);
    }
}
