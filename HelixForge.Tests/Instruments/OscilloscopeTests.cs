using HelixForge.Instruments;

namespace HelixForge.Tests.Instruments;

public class OscilloscopeTests
{
    [Fact]
    public void Initialize_SetsIsInitialized()
    {
        var scope = new SimOscilloscopeDevice("scope-01", new OscilloscopeSimConfig());
        Assert.False(scope.IsInitialized);

        scope.Initialize();

        Assert.True(scope.IsInitialized);
    }

    [Fact]
    public void ReadCapture_ReturnsConfiguredChannels()
    {
        var config = new OscilloscopeSimConfig { ChannelCount = 4, SampleRate = 250000.0 };
        var scope = new SimOscilloscopeDevice("scope-01", config);
        scope.Initialize();

        var data = scope.ReadCapture();

        Assert.Equal(4, data.ChannelCount);
        Assert.Equal("CH1", data.Channels[0].ChannelName);
        Assert.Equal(1024, data.Channels[0].SampleCount);
        Assert.Equal(250000.0, data.SampleRate);
    }

    [Fact]
    public void ReadCapture_SineWave_WithinVoltageRange()
    {
        var config = new OscilloscopeSimConfig
        {
            Amplitude = 3.3,
            VoltageRange = 5.0,
            WaveformShape = OscilloscopeWaveformShape.Sine,
            NoiseAmplitude = 0.0
        };
        var scope = new SimOscilloscopeDevice("scope-01", config);
        scope.Initialize();

        var data = scope.ReadCapture();

        foreach (double sample in data.Channels[0].Samples)
        {
            Assert.InRange(sample, -5.0, 5.0);
        }
    }

    [Fact]
    public void SameConfig_ProducesIdenticalCaptures()
    {
        var config = new OscilloscopeSimConfig { NoiseAmplitude = 0.5, RandomSeed = 42 };
        var scope1 = new SimOscilloscopeDevice("scope-1", config);
        var scope2 = new SimOscilloscopeDevice("scope-2", config);
        scope1.Initialize();
        scope2.Initialize();

        var data1 = scope1.ReadCapture();
        var data2 = scope2.ReadCapture();

        Assert.Equal(data1.Channels[0].Samples[10], data2.Channels[0].Samples[10], 10);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentNoise()
    {
        var config1 = new OscilloscopeSimConfig { NoiseAmplitude = 0.5, RandomSeed = 1 };
        var config2 = new OscilloscopeSimConfig { NoiseAmplitude = 0.5, RandomSeed = 2 };
        var scope1 = new SimOscilloscopeDevice("scope-1", config1);
        var scope2 = new SimOscilloscopeDevice("scope-2", config2);
        scope1.Initialize();
        scope2.Initialize();

        var data1 = scope1.ReadCapture();
        var data2 = scope2.ReadCapture();

        bool anyDifferent = false;
        for (int i = 0; i < data1.Channels[0].SampleCount; i++)
        {
            if (data1.Channels[0].Samples[i] != data2.Channels[0].Samples[i])
            {
                anyDifferent = true;
                break;
            }
        }

        Assert.True(anyDifferent, "Different seeds should produce different noisy samples");
    }
}