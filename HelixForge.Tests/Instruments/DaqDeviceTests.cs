using HelixForge.Instruments;

namespace HelixForge.Tests.Instruments;

public class DaqDeviceTests
{
    [Fact]
    public void ReadScan_ReturnsConfiguredChannels()
    {
        var config = new DaqSimConfig { ChannelCount = 8, BaselineVoltage = 1.0 };
        var daq = new SimDaqDevice("daq-01", config);
        daq.Initialize();

        var scan = daq.ReadScan();

        Assert.Equal(8, scan.Channels.Length);
        Assert.Equal("AI0", scan.Channels[0].ChannelName);
    }

    [Fact]
    public void ReadScan_WithinVoltageRange()
    {
        var config = new DaqSimConfig { VoltageRange = 10.0, Amplitude = 5.0 };
        var daq = new SimDaqDevice("daq-01", config);
        daq.Initialize();

        var scan = daq.ReadScan();

        foreach (var channel in scan.Channels)
        {
            Assert.InRange(channel.Voltage, -10.0, 10.0);
        }
    }

    [Fact]
    public void SameConfig_ProducesIdenticalScans()
    {
        var config = new DaqSimConfig { NoiseAmplitude = 0.5, RandomSeed = 7 };
        var daq1 = new SimDaqDevice("daq-1", config);
        var daq2 = new SimDaqDevice("daq-2", config);
        daq1.Initialize();
        daq2.Initialize();

        var scan1 = daq1.ReadScan();
        var scan2 = daq2.ReadScan();

        Assert.Equal(scan1.Channels[3].Voltage, scan2.Channels[3].Voltage, 10);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentNoise()
    {
        var config1 = new DaqSimConfig { NoiseAmplitude = 0.5, RandomSeed = 3 };
        var config2 = new DaqSimConfig { NoiseAmplitude = 0.5, RandomSeed = 4 };
        var daq1 = new SimDaqDevice("daq-1", config1);
        var daq2 = new SimDaqDevice("daq-2", config2);
        daq1.Initialize();
        daq2.Initialize();

        var scan1 = daq1.ReadScan();
        var scan2 = daq2.ReadScan();

        bool anyDifferent = false;
        for (int i = 0; i < scan1.Channels.Length; i++)
        {
            if (scan1.Channels[i].Voltage != scan2.Channels[i].Voltage)
            {
                anyDifferent = true;
                break;
            }
        }

        Assert.True(anyDifferent, "Different seeds should produce different noisy channels");
    }
}