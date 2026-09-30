using System;
using HelixForge;

namespace HelixForge.Instruments;

/// <summary>
/// Deterministic simulated DAQ. Each call to ReadScan advances an internal
/// sample counter, so identical configurations and equal call counts yield
/// identical scans.
/// </summary>
public sealed class SimDaqDevice : IDaqDevice
{
    private readonly DaqSimConfig _config;
    private readonly ITelemetryPublisher? _telemetry;
    private bool _initialized;
    private long _sampleIndex;
    private uint _noiseState;

    /// <summary>
    /// Creates a new simulated DAQ device.
    /// </summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="config">Simulation configuration.</param>
    /// <param name="telemetry">Optional telemetry publisher receiving per-channel metrics.</param>
    public SimDaqDevice(string deviceId, DaqSimConfig config, ITelemetryPublisher? telemetry = null)
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
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");
        _initialized = true;
    }

    /// <inheritdoc/>
    public DaqScan ReadScan()
    {
        var channels = new DaqChannelData[_config.ChannelCount];
        for (int c = 0; c < _config.ChannelCount; c++)
        {
            double t = _sampleIndex / _config.SampleRate;
            double signal = _config.BaselineVoltage
                + _config.Amplitude * Math.Sin(2.0 * Math.PI * SignalFrequency(c) * t + ChannelPhase(c));
            if (_config.NoiseAmplitude > 0.0)
                signal += _config.NoiseAmplitude * NextNoise();
            double voltage = Clamp(signal, -_config.VoltageRange, _config.VoltageRange);
            var sampledAt = TimeSpan.FromSeconds(t);
            channels[c] = new DaqChannelData($"AI{c}", voltage, sampledAt);
            _telemetry?.Publish(DeviceId, $"daq.ch{c}.voltage", voltage, sampledAt);
        }

        var scan = new DaqScan(DeviceId, channels, TimeSpan.FromSeconds(channels[0].SampledAt.TotalSeconds));
        _sampleIndex++;
        return scan;
    }

    private static double SignalFrequency(int channel) => 1.0 + channel * 1.5;

    private static double ChannelPhase(int channel) => channel * 0.35;

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