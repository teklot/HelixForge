using System;

namespace HelixForge.Simulation;

/// <summary>
/// Simulated servo actuator with configurable angle limits and slew-rate dynamics.
/// </summary>
public sealed class SimServoDevice : IServoDevice
{
    private readonly ServoSimConfig _config;
    private double _currentAngle;
    private double _targetAngle;
    private bool _initialized;

    /// <summary>
    /// Creates a new simulated servo device.
    /// </summary>
    /// <param name="deviceId">Unique identifier for this device.</param>
    /// <param name="config">Simulation configuration for servo dynamics.</param>
    public SimServoDevice(string deviceId, ServoSimConfig config)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _currentAngle = config.InitialAngle;
        _targetAngle = config.InitialAngle;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public bool IsActive => _initialized;

    /// <inheritdoc/>
    public double Angle => _currentAngle;

    /// <inheritdoc/>
    public double TargetAngle => _targetAngle;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        _initialized = true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _currentAngle = _config.InitialAngle;
        _targetAngle = _config.InitialAngle;
    }

    /// <inheritdoc/>
    public void SetAngle(double degrees)
    {
        _targetAngle = Clamp(degrees, _config.MinAngle, _config.MaxAngle);
    }

    /// <summary>
    /// Advances the servo state toward its target at the configured slew rate.
    /// </summary>
    /// <param name="deltaTime">The elapsed time since the last update.</param>
    public void Update(TimeSpan deltaTime)
    {
        if (!_initialized)
            return;

        double dt = deltaTime.TotalSeconds;
        double maxTravel = _config.SlewRate * dt;
        double delta = _targetAngle - _currentAngle;
        delta = Clamp(delta, -maxTravel, maxTravel);
        _currentAngle += delta;
    }

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : (value > max ? max : value);

    /// <inheritdoc/>
    public void Dispose() { }
}
