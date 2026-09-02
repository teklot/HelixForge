using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class EnvironmentTests
{
    [Fact]
    public void Gps_WindVelocity_AddsToIntegratedPosition()
    {
        var env = new EnvironmentSimConfig { WindVelocity = new Vector3(1.0, 0.0, 0.0), RandomSeed = 42 };
        var config = new GpsSimConfig { PositionNoise = 0.0, RandomSeed = 42, Environment = env };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        // 1 m/s North for 10s = 10m of latitude displacement (~0.00009 deg).
        for (int i = 0; i < 100; i++)
            gps.Update(TimeSpan.FromMilliseconds(100));

        double expectedDeg = 10.0 / 111320.0;
        Assert.True(gps.Read().Latitude > expectedDeg * 0.5);
    }

    [Fact]
    public void Gps_EnvReferenceAltitude_SetsInitialAltitude()
    {
        var env = new EnvironmentSimConfig { ReferenceAltitudeMeters = 150.0 };
        var config = new GpsSimConfig { Environment = env };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        Assert.Equal(150.0, gps.Read().Altitude, 4);
    }

    [Fact]
    public void Barometer_EnvReferenceAltitude_SetsInitialAltitude()
    {
        var env = new EnvironmentSimConfig { ReferenceAltitudeMeters = 75.0 };
        var config = new BarometerSimConfig { AltitudeNoise = 0.0, Environment = env };
        var baro = new SimBarometerDevice("baro-01", config);
        baro.Initialize();

        Assert.Equal(75.0, baro.Read().Altitude, 4);
    }

    [Fact]
    public void Gps_NoEnvironment_PositionUnchanged()
    {
        var config = new GpsSimConfig { PositionNoise = 0.0, RandomSeed = 1 };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        for (int i = 0; i < 100; i++)
            gps.Update(TimeSpan.FromMilliseconds(100));

        Assert.Equal(0.0, gps.Read().Latitude, 9);
        Assert.Equal(0.0, gps.Read().Longitude, 9);
    }
}
