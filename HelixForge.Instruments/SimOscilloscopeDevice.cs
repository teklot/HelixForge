using System;
using HelixForge;

namespace HelixForge.Instruments;

/// <summary>
/// Deterministic simulated oscilloscope. Produces waveform captures whose
/// values depend only on device configuration and simulation clock time.
/// </summary>
public sealed class SimOscilloscopeDevice : IOscilloscopeDevice
{
    private readonly OscilloscopeSimConfig _config;
    private readonly ITelemetryPublisher? _telemetry;
    private readonly int _captureLength = 1024;
    private bool _initialized;

    private uint _noiseState;

    /// <summary>
    /// Creates a new simulated oscilloscope.
    /// </summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="config">Simulation configuration.</param>
    /// <param name="telemetry">Optional telemetry publisher receiving per-channel metrics.</param>
    public SimOscilloscopeDevice(string deviceId, OscilloscopeSimConfig config, ITelemetryPublisher? telemetry = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _telemetry = telemetry;
        _noiseState = unchecked((uint)config.RandomSeed);
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public int ChannelCount => _config.ChannelCount;

    /// <inheritdoc/>
    public double SampleRate => _config.SampleRate;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");
        _initialized = true;
    }

    /// <inheritdoc/>
    public void Arm() { }

    /// <inheritdoc/>
    public OscilloscopeData ReadCapture()
    {
        var channels = new OscilloscopeChannelData[_config.ChannelCount];
        for (int c = 0; c < _config.ChannelCount; c++)
        {
            var samples = new double[_captureLength];
            for (int i = 0; i < _captureLength; i++)
                samples[i] = ComputeSample(c, i);

            channels[c] = new OscilloscopeChannelData($"CH{c + 1}", samples);
            _telemetry?.Publish(DeviceId, $"scope.ch{c + 1}.peak", Peak(samples), TimeSpan.Zero);
            _telemetry?.Publish(DeviceId, $"scope.ch{c + 1}.rms", Rms(samples), TimeSpan.Zero);
        }

        return new OscilloscopeData(DeviceId, channels, _config.SampleRate, TimeSpan.Zero);
    }

    private static double Peak(double[] samples)
    {
        double peak = 0.0;
        for (int i = 0; i < samples.Length; i++)
            peak = Math.Max(peak, Math.Abs(samples[i]));
        return peak;
    }

    private static double Rms(double[] samples)
    {
        double sum = 0.0;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];
        return Math.Sqrt(sum / samples.Length);
    }

    private double ComputeSample(int channel, int index)
    {
        double phase = 2.0 * Math.PI * _config.FrequencyHz * index / _config.SampleRate;
        double value = _config.WaveformShape switch
        {
            OscilloscopeWaveformShape.Square => Math.Sign(Math.Sin(phase + ChannelOffset(channel))),
            OscilloscopeWaveformShape.Triangle => 2.0 / Math.PI * Math.Asin(Math.Sin(phase + ChannelOffset(channel))),
            OscilloscopeWaveformShape.Sawtooth => 2.0 * ((phase / (2.0 * Math.PI)) - Math.Floor(0.5 + phase / (2.0 * Math.PI))),
            _ => Math.Sin(phase + ChannelOffset(channel))
        };

        double scaled = value * _config.Amplitude;
        if (_config.NoiseAmplitude > 0.0)
            scaled += _config.NoiseAmplitude * NextNoise();

        return Clamp(scaled, -_config.VoltageRange, _config.VoltageRange);
    }

    private static double ChannelOffset(int channel) => channel * 0.7;

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : (value > max ? max : value);

    private double NextNoise()
    {
        _noiseState = _noiseState * 1664525u + 1013904223u;
        return (_noiseState / 4294967296.0) * 2.0 - 1.0;
    }

    /// <inheritdoc/>
    public void Dispose() { }
}
