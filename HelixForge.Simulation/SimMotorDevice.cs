using System;

namespace HelixForge.Simulation;

/// <summary>
/// Simulated motor/ESC device with configurable response dynamics.
/// </summary>
public sealed class SimMotorDevice : IMotorDevice
{
    private readonly MotorSimConfig _config;
    private double _currentThrottle;
    private double _targetThrottle;
    private double _currentRpm;
    private bool _initialized;

    /// <summary>
    /// Creates a new simulated motor device.
    /// </summary>
    /// <param name="deviceId">Unique identifier for this device.</param>
    /// <param name="config">Simulation configuration for motor dynamics.</param>
    public SimMotorDevice(string deviceId, MotorSimConfig config)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _currentThrottle = config.InitialThrottle;
        _targetThrottle = config.InitialThrottle;
        _currentRpm = config.InitialThrottle * config.MaxRpm;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public bool IsActive => _initialized;

    /// <inheritdoc/>
    public double Throttle => _currentThrottle;

    /// <summary>Gets the current simulated RPM.</summary>
    public double CurrentRpm => _currentRpm;

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
        _currentThrottle = _config.InitialThrottle;
        _targetThrottle = _config.InitialThrottle;
        _currentRpm = _config.InitialThrottle * _config.MaxRpm;
    }

    /// <inheritdoc/>
    public void SetThrottle(double normalizedThrottle)
    {
        if (normalizedThrottle < 0.0 || normalizedThrottle > 1.0)
            throw new ArgumentOutOfRangeException(nameof(normalizedThrottle), "Throttle must be between 0.0 and 1.0.");

        _targetThrottle = normalizedThrottle;
    }

    /// <summary>
    /// Advances the motor state by the specified elapsed time.
    /// </summary>
    /// <param name="deltaTime">The elapsed time since the last update.</param>
    public void Update(TimeSpan deltaTime)
    {
        if (!_initialized)
            return;

        double dt = deltaTime.TotalSeconds;
        double alpha = 1.0 - Math.Exp(-dt / _config.ResponseTimeConstant);
        _currentThrottle += (_targetThrottle - _currentThrottle) * alpha;

        if (Math.Abs(_currentThrottle) < _config.Deadband)
            _currentThrottle = 0.0;

        _currentRpm = _currentThrottle * _config.MaxRpm;
    }

    /// <inheritdoc/>
    public void Dispose() { }
}
