using System;
using HelixForge;

namespace HelixForge.Instruments;

/// <summary>
/// Deterministic simulated bench power supply. The output voltage ramps
/// toward the target by a fixed step per clock tick and the current is
/// derived from the reflected load resistance.
/// </summary>
public sealed class SimBenchPsuDevice : IBenchPowerSupply
{
    private readonly BenchPsuSimConfig _config;
    private readonly ITelemetryPublisher? _telemetry;
    private bool _initialized;
    private bool _outputEnabled;
    private double _targetVoltage;
    private double _outputVoltage;

    /// <summary>
    /// Creates a new simulated bench power supply.
    /// </summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="config">Simulation configuration.</param>
    /// <param name="telemetry">Optional telemetry publisher receiving voltage/current metrics.</param>
    public SimBenchPsuDevice(string deviceId, BenchPsuSimConfig config, ITelemetryPublisher? telemetry = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _telemetry = telemetry;
        _targetVoltage = Clamp(config.DefaultVoltage, 0.0, config.MaxVoltage);
        _outputVoltage = 0.0;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public bool OutputEnabled => _outputEnabled;

    /// <inheritdoc/>
    public double TargetVoltage => _targetVoltage;

    /// <inheritdoc/>
    public double OutputVoltage => _outputVoltage;

    /// <inheritdoc/>
    public double OutputCurrent => _outputVoltage / _config.LoadResistance;

    /// <inheritdoc/>
    public double MaxVoltage => _config.MaxVoltage;

    /// <inheritdoc/>
    public double MaxCurrent => _config.MaxCurrent;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");
        _initialized = true;
    }

    /// <inheritdoc/>
    public void SetOutputEnabled(bool enabled) => _outputEnabled = enabled;

    /// <inheritdoc/>
    public void SetVoltage(double volts) => _targetVoltage = Clamp(volts, 0.0, _config.MaxVoltage);

    /// <inheritdoc/>
    public void UpdateClock()
    {
        if (!_outputEnabled)
        {
            _outputVoltage = 0.0;
            return;
        }

        double delta = _targetVoltage - _outputVoltage;
        if (delta > 0)
            _outputVoltage += Math.Min(delta, _config.VoltageStepPerClock);
        else
            _outputVoltage -= Math.Min(-delta, _config.VoltageStepPerClock);

        _telemetry?.Publish(DeviceId, "psu.voltage", _outputVoltage, TimeSpan.Zero);
        _telemetry?.Publish(DeviceId, "psu.current", OutputCurrent, TimeSpan.Zero);
    }

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : (value > max ? max : value);

    /// <inheritdoc/>
    public void Dispose() { }
}