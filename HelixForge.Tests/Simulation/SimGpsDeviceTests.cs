using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class SimGpsDeviceTests
{
    [Fact]
    public void Initialize_SetsIsInitialized()
    {
        var gps = new SimGpsDevice("gps-01", new GpsSimConfig());
        Assert.False(gps.IsInitialized);

        gps.Initialize();

        Assert.True(gps.IsInitialized);
    }

    [Fact]
    public void Update_AdvancesTime()
    {
        var gps = new SimGpsDevice("gps-01", new GpsSimConfig());
        gps.Initialize();

        gps.Update(TimeSpan.FromSeconds(1));

        var data = gps.Read();
        Assert.Equal(TimeSpan.FromSeconds(1), data.Timestamp);
    }

    [Fact]
    public void SetVelocity_MovesPosition()
    {
        var gps = new SimGpsDevice("gps-01", new GpsSimConfig());
        gps.Initialize();

        // Move north at 10 m/s for 1 second
        gps.SetVelocity(new Vector3(10.0, 0.0, 0.0));
        gps.Update(TimeSpan.FromSeconds(1));

        var data = gps.Read();
        Assert.True(data.Latitude > 0.0); // Should have moved north
    }

    [Fact]
    public void Reset_ReturnsToInitialState()
    {
        var config = new GpsSimConfig { InitialLatitude = 37.7749, InitialLongitude = -122.4194 };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        gps.SetVelocity(new Vector3(100.0, 0.0, 0.0));
        gps.Update(TimeSpan.FromSeconds(10));
        gps.Reset();

        var data = gps.Read();
        Assert.Equal(37.7749, data.Latitude, 4);
        Assert.Equal(-122.4194, data.Longitude, 4);
    }
}
