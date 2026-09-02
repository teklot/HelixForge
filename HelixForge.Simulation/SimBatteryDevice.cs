using System;
using System.Collections.Generic;

namespace HelixForge.Simulation;

/// <summary>
/// Simulated battery with an open-circuit voltage curve, internal-resistance sag
/// under load, and capacity drain proportional to drawn current.
/// </summary>
public sealed class SimBatteryDevice : IBatteryDevice
{
    private readonly BatterySimConfig _config;
    private readonly IReadOnlyCollection<ICurrentConsumer> _consumers;
    private double _chargeFraction;
    private TimeSpan _currentTime;
    private bool _initialized;

    /// <summary>
    /// Creates a new simulated battery device.
    /// </summary>
    /// <param name="deviceId">Unique identifier for this device.</param>
    /// <param name="config">Simulation configuration for battery behavior.</param>
    /// <param name="consumers">Current-consuming devices whose draw drives sag and drain. May be empty.</param>
    public SimBatteryDevice(string deviceId, BatterySimConfig config, IReadOnlyCollection<ICurrentConsumer>? consumers = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _consumers = consumers ?? Array.Empty<ICurrentConsumer>();
        _chargeFraction = config.InitialCharge;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public BatteryData LatestReading { get; private set; } = BatteryData.Empty;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        _initialized = true;
        _currentTime = TimeSpan.Zero;
        _chargeFraction = _config.InitialCharge;
        LatestReading = ComputeReading(TotalCurrent(), _currentTime);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _currentTime = TimeSpan.Zero;
        _chargeFraction = _config.InitialCharge;
        LatestReading = ComputeReading(TotalCurrent(), _currentTime);
    }

    /// <summary>
    /// Advances the battery state, draining capacity by the total drawn current.
    /// </summary>
    /// <param name="deltaTime">The elapsed time since the last update.</param>
    public void Update(TimeSpan deltaTime)
    {
        if (!_initialized)
            return;

        _currentTime += deltaTime;

        double current = TotalCurrent();
        double dtHours = deltaTime.TotalSeconds / 3600.0;
        _chargeFraction -= (current * dtHours) / _config.CapacityAh;
        if (_chargeFraction < 0.0)
            _chargeFraction = 0.0;

        LatestReading = ComputeReading(current, _currentTime);
    }

    /// <inheritdoc/>
    public BatteryData Read() => LatestReading;

    /// <inheritdoc/>
    public void Dispose() { }

    private double TotalCurrent()
    {
        double total = 0.0;
        foreach (var consumer in _consumers)
            total += consumer.Current;
        return total;
    }

    private BatteryData ComputeReading(double current, TimeSpan timestamp)
    {
        double soc = _chargeFraction;
        double ocv = _config.EmptyVoltage + (_config.FullVoltage - _config.EmptyVoltage) * soc;
        double sag = current * _config.InternalResistance;
        double voltage = Math.Max(0.0, ocv - sag);

        return new BatteryData(voltage, current, soc, timestamp);
    }
}
