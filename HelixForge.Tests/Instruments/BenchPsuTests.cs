using HelixForge.Instruments;

namespace HelixForge.Tests.Instruments;

public class BenchPsuTests
{
    [Fact]
    public void Initialize_SetsIsInitialized()
    {
        var psu = new SimBenchPsuDevice("psu-01", new BenchPsuSimConfig());
        Assert.False(psu.IsInitialized);

        psu.Initialize();

        Assert.True(psu.IsInitialized);
    }

    [Fact]
    public void SetVoltage_ClampsToMax()
    {
        var config = new BenchPsuSimConfig { MaxVoltage = 30.0 };
        var psu = new SimBenchPsuDevice("psu-01", config);
        psu.Initialize();

        psu.SetVoltage(60.0);

        Assert.Equal(30.0, psu.TargetVoltage, 6);
    }

    [Fact]
    public void UpdateClock_DischargedWhenDisabled()
    {
        var config = new BenchPsuSimConfig { DefaultVoltage = 12.0 };
        var psu = new SimBenchPsuDevice("psu-01", config);
        psu.Initialize();
        psu.SetOutputEnabled(false);

        psu.UpdateClock();

        Assert.Equal(0.0, psu.OutputVoltage, 6);
    }

    [Fact]
    public void UpdateClock_RampsTowardTarget()
    {
        var config = new BenchPsuSimConfig
        {
            DefaultVoltage = 12.0,
            VoltageStepPerClock = 1.0,
            LoadResistance = 10.0
        };
        var psu = new SimBenchPsuDevice("psu-01", config);
        psu.Initialize();
        psu.SetOutputEnabled(true);

        psu.UpdateClock();
        Assert.Equal(1.0, psu.OutputVoltage, 6);

        psu.UpdateClock();
        Assert.Equal(2.0, psu.OutputVoltage, 6);
    }

    [Fact]
    public void OutputCurrent_DerivedFromLoad()
    {
        var config = new BenchPsuSimConfig { LoadResistance = 10.0, VoltageStepPerClock = 10.0 };
        var psu = new SimBenchPsuDevice("psu-01", config);
        psu.Initialize();
        psu.SetOutputEnabled(true);

        psu.SetVoltage(5.0);
        psu.UpdateClock();
        Assert.Equal(0.5, psu.OutputCurrent, 6);
    }
}