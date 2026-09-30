using HelixForge.Instruments;

namespace HelixForge.Tests.Instruments;

public class BatteryCyclerTests
{
    [Fact]
    public void Initialize_SetsIsInitialized()
    {
        var cycler = new SimBatteryCyclerDevice("cyc-01", new BatteryCyclerSimConfig());
        Assert.False(cycler.IsInitialized);

        cycler.Initialize();

        Assert.True(cycler.IsInitialized);
    }

    [Fact]
    public void UpdateClock_ChargingRaisesChargeFraction()
    {
        var config = new BatteryCyclerSimConfig
        {
            CapacityAh = 1.0,
            TimeStepSeconds = 3600.0, // 1 hour per tick
            InitialChargeFraction = 0.0
        };
        var cycler = new SimBatteryCyclerDevice("cyc-01", config);
        cycler.Initialize();
        cycler.SetCurrent(0.5); // 0.5A for 1h = 0.5Ah

        cycler.UpdateClock();

        Assert.Equal(0.5, cycler.ChargeFraction, 6);
    }

    [Fact]
    public void UpdateClock_DischargingLowersChargeFraction()
    {
        var config = new BatteryCyclerSimConfig
        {
            CapacityAh = 1.0,
            TimeStepSeconds = 1800.0,
            InitialChargeFraction = 0.5
        };
        var cycler = new SimBatteryCyclerDevice("cyc-01", config);
        cycler.Initialize();
        cycler.SetCurrent(-1.0); // 1A discharge

        cycler.UpdateClock();

        Assert.Equal(0.0, cycler.ChargeFraction, 6);
    }

    [Fact]
    public void ChargeFraction_ClampsAtBounds()
    {
        var config = new BatteryCyclerSimConfig
        {
            CapacityAh = 1.0,
            TimeStepSeconds = 3600.0,
            InitialChargeFraction = 0.99
        };
        var cycler = new SimBatteryCyclerDevice("cyc-01", config);
        cycler.Initialize();
        cycler.SetCurrent(5.0);

        cycler.UpdateClock();

        Assert.Equal(1.0, cycler.ChargeFraction, 6);
    }

    [Fact]
    public void CellVoltage_RisesWithHigherCharge()
    {
        var config = new BatteryCyclerSimConfig { NominalVoltage = 3.7 };
        var cyclerLow = new SimBatteryCyclerDevice("cyc-a", new BatteryCyclerSimConfig { InitialChargeFraction = 0.1 });
        cyclerLow.Initialize();
        var cyclerHigh = new SimBatteryCyclerDevice("cyc-b", new BatteryCyclerSimConfig { InitialChargeFraction = 0.9 });
        cyclerHigh.Initialize();

        Assert.True(cyclerHigh.CellVoltage > cyclerLow.CellVoltage);
    }
}