using System;

namespace HelixForge.Simulation;

/// <summary>
/// Deterministic, time-step-based simulation coordinator.
/// Advances virtual time and calls Update on registered sensors each step.
/// Does not depend on telemetry; observation is via events or ITelemetryPublisher injection.
/// </summary>
public sealed class SimulationEngine
{
    private readonly DeviceRegistry _registry;
    private readonly ITelemetryPublisher? _telemetry;
    private readonly SimulationConfig _config;
    private TimeSpan _currentTime;
    private int _stepCount;

    /// <summary>
    /// Creates a new simulation engine.
    /// </summary>
    /// <param name="registry">Device registry containing all devices to simulate.</param>
    /// <param name="telemetry">Optional telemetry publisher for observation.</param>
    /// <param name="config">Simulation configuration.</param>
    public SimulationEngine(DeviceRegistry registry, ITelemetryPublisher? telemetry, SimulationConfig config)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _telemetry = telemetry;
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _currentTime = config.StartTime;
    }

    /// <summary>Current simulation time.</summary>
    public TimeSpan CurrentTime => _currentTime;

    /// <summary>Number of steps executed so far.</summary>
    public int StepCount => _stepCount;

    /// <summary>Time step configured for this engine.</summary>
    public TimeSpan TimeStep => _config.TimeStep;

    /// <summary>Fires after each simulation step completes.</summary>
    public event EventHandler<SimulationStepCompletedEventArgs>? StepCompleted;

    /// <summary>
    /// Advances the simulation by one time step.
    /// Updates all sensors and motor devices in the registry.
    /// </summary>
    public void Step()
    {
        foreach (var sensor in _registry.GetAllByType<ISensor>())
            sensor.Update(_config.TimeStep);

        foreach (var motor in _registry.GetAllByType<SimMotorDevice>())
            motor.Update(_config.TimeStep);

        _currentTime += _config.TimeStep;
        _stepCount++;

        PublishTelemetry();
        StepCompleted?.Invoke(this, new SimulationStepCompletedEventArgs(_currentTime, _stepCount, _config.TimeStep));
    }

    /// <summary>
    /// Runs the simulation for the specified duration.
    /// </summary>
    /// <param name="duration">Total simulation time to execute.</param>
    public void Run(TimeSpan duration)
    {
        var endTime = _currentTime + duration;
        int iterationsThisRun = 0;
        while (_currentTime < endTime)
        {
            if (_config.MaxIterations > 0 && iterationsThisRun >= _config.MaxIterations)
                break;
            Step();
            iterationsThisRun++;
        }
    }

    /// <summary>
    /// Runs the simulation for the specified duration with a callback invoked after each step.
    /// </summary>
    /// <param name="duration">Total simulation time to execute.</param>
    /// <param name="onStep">Callback invoked after each step with the current simulation time.</param>
    public void Run(TimeSpan duration, Action<TimeSpan> onStep)
    {
        var endTime = _currentTime + duration;
        int iterationsThisRun = 0;
        while (_currentTime < endTime)
        {
            if (_config.MaxIterations > 0 && iterationsThisRun >= _config.MaxIterations)
                break;
            Step();
            iterationsThisRun++;
            onStep(_currentTime);
        }
    }

    /// <summary>
    /// Resets the simulation engine and all registered devices to their initial state.
    /// </summary>
    public void Reset()
    {
        _currentTime = _config.StartTime;
        _stepCount = 0;

        foreach (var device in _registry.GetAllDevices())
            device.Reset();
    }

    private void PublishTelemetry()
    {
        if (_telemetry == null)
            return;

        foreach (var imu in _registry.GetAllByType<IImuDevice>())
        {
            var data = imu.Read();
            _telemetry.Publish(imu.DeviceId, "orientation.x", data.Orientation.X, _currentTime);
            _telemetry.Publish(imu.DeviceId, "orientation.y", data.Orientation.Y, _currentTime);
            _telemetry.Publish(imu.DeviceId, "orientation.z", data.Orientation.Z, _currentTime);
            var angVel = data.AngularVelocity;
            var accel = data.Acceleration;
            _telemetry.Publish(imu.DeviceId, "angular_velocity", in angVel, _currentTime);
            _telemetry.Publish(imu.DeviceId, "acceleration", in accel, _currentTime);
        }

        foreach (var motor in _registry.GetAllByType<IMotorDevice>())
            _telemetry.Publish(motor.DeviceId, "throttle", motor.Throttle, _currentTime);

        foreach (var gps in _registry.GetAllByType<IGpsDevice>())
        {
            var data = gps.Read();
            _telemetry.Publish(gps.DeviceId, "latitude", data.Latitude, _currentTime);
            _telemetry.Publish(gps.DeviceId, "longitude", data.Longitude, _currentTime);
            _telemetry.Publish(gps.DeviceId, "altitude", data.Altitude, _currentTime);
        }
    }
}
