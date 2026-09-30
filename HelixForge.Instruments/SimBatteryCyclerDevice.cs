using System;
using HelixForge;

namespace HelixForge.Instruments;

/// <summary>
/// Deterministic simulated battery cycler. Integrates applied current into
/// state of charge and reports an open-circuit-voltage-based cell voltage.
/// </summary>
public sealed class SimBatteryCyclerDevice : IBatteryCycler
{
    private readonly BatteryCyclerSimConfig _config;
    private readonly ITelemetryPublisher? _telemetry;
    private bool _initialized;
    private double _setPointCurrent;
    private double _chargeFraction;

    /// <summary>
    /// Creates a new simulated battery cycler.
    /// </summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="config">Simulation configuration.</param>
    /// <param name="telemetry">Optional telemetry publisher receiving SOC/voltage metrics.</param>
    public SimBatteryCyclerDevice(string deviceId, BatteryCyclerSimConfig config, ITelemetryPublisher? telemetry = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _telemetry = telemetry;
        _chargeFraction = Clamp(config.InitialChargeFraction, 0.0, 1.0);
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public double SetPointCurrent => _setPointCurrent;

    /// <inheritdoc/>
    public double ChargeFraction => _chargeFraction;

    /// <inheritdoc/>
    public double CellVoltage => _config.NominalVoltage + (_chargeFraction - 0.5) * 0.8;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");
        _initialized = true;
    }

    /// <inheritdoc/>
    public void SetCurrent(double amperes) => _setPointCurrent = amperes;

    /// <inheritdoc/>
    public void UpdateClock()
    {
        double coulombsPerTick = _setPointCurrent * _config.TimeStepSeconds / 3600.0;
        double capacity = _config.CapacityAh;
        _chargeFraction = Clamp(_chargeFraction + coulombsPerTick / capacity, 0.0, 1.0);

        _telemetry?.Publish(DeviceId, "cycler.soc", _chargeFraction, TimeSpan.Zero);
        _telemetry?.Publish(DeviceId, "cycler.voltage", CellVoltage, TimeSpan.Zero);
        _telemetry?.Publish(DeviceId, "cycler.current", _setPointCurrent, TimeSpan.Zero);
    }

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : (value > max ? max : value);

    /// <inheritdoc/>
    public void Dispose() { }
}