using HelixForge.Instruments;

namespace HelixForge.Tests.Instruments;

public class PlcDeviceTests
{
    [Fact]
    public void Initialize_SetsIsInitialized()
    {
        var plc = new SimPlcDevice("plc-01", 4, 2);
        Assert.False(plc.IsInitialized);

        plc.Initialize();

        Assert.True(plc.IsInitialized);
    }

    [Fact]
    public void ScanOnce_AndRungSumsAndOdds()
    {
        var plc = new SimPlcDevice("plc-01", 4, 2);
        plc.Initialize();
        plc.SetInput(0, true);
        plc.SetInput(1, true);
        plc.SetInput(2, true);

        plc.ScanOnce();

        Assert.True(plc.ReadOutput(0));  // I0 && I1
        Assert.True(plc.ReadOutput(1));  // I2 || I3
    }

    [Fact]
    public void ScanOnce_AndRungRequiresBoth()
    {
        var plc = new SimPlcDevice("plc-01", 4, 2);
        plc.Initialize();
        plc.SetInput(0, true);
        plc.SetInput(1, false);

        plc.ScanOnce();

        Assert.False(plc.ReadOutput(0));
    }
}