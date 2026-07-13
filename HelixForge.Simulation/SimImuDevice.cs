using System;

namespace HelixForge.Simulation;

/// <summary>
/// Simulated IMU device with configurable noise and drift models.
/// Produces deterministic output based on internal state.
/// </summary>
public sealed class SimImuDevice : IImuDevice
{
    private readonly ImuSimConfig _config;
    private readonly Random _random;
    private Vector3 _orientation;
    private Vector3 _angularVelocity;
    private Vector3 _acceleration;
    private Vector3 _accelDrift;
    private Vector3 _gyroDrift;
    private TimeSpan _currentTime;
    private bool _initialized;

    /// <summary>
    /// Creates a new simulated IMU device.
    /// </summary>
    /// <param name="deviceId">Unique identifier for this device.</param>
    /// <param name="config">Simulation configuration for noise and drift.</param>
    /// <param name="random">Random number generator for noise. If null, creates one from config seed.</param>
    public SimImuDevice(string deviceId, ImuSimConfig config, Random? random = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _random = random ?? new Random(config.RandomSeed ?? Environment.TickCount);
        _orientation = config.InitialOrientation;
        _angularVelocity = Vector3.Zero;
        _acceleration = new Vector3(0, 0, -9.81);
        _accelDrift = Vector3.Zero;
        _gyroDrift = Vector3.Zero;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public ImuData LatestReading { get; private set; } = ImuData.Empty;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        _initialized = true;
        _currentTime = TimeSpan.Zero;
        LatestReading = new ImuData(_orientation, _angularVelocity, _acceleration, _currentTime);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _orientation = _config.InitialOrientation;
        _angularVelocity = Vector3.Zero;
        _acceleration = new Vector3(0, 0, -9.81);
        _accelDrift = Vector3.Zero;
        _gyroDrift = Vector3.Zero;
        _currentTime = TimeSpan.Zero;
        LatestReading = new ImuData(_orientation, _angularVelocity, _acceleration, _currentTime);
    }

    /// <inheritdoc/>
    public void Update(TimeSpan deltaTime)
    {
        if (!_initialized)
            return;

        double dt = deltaTime.TotalSeconds;
        _currentTime += deltaTime;

        _accelDrift = new Vector3(
            _accelDrift.X + _config.AccelerometerDrift * dt * GaussianRandom(),
            _accelDrift.Y + _config.AccelerometerDrift * dt * GaussianRandom(),
            _accelDrift.Z + _config.AccelerometerDrift * dt * GaussianRandom());

        _gyroDrift = new Vector3(
            _gyroDrift.X + _config.GyroscopeDrift * dt * GaussianRandom(),
            _gyroDrift.Y + _config.GyroscopeDrift * dt * GaussianRandom(),
            _gyroDrift.Z + _config.GyroscopeDrift * dt * GaussianRandom());

        _orientation = new Vector3(
            _orientation.X + _angularVelocity.X * dt,
            _orientation.Y + _angularVelocity.Y * dt,
            _orientation.Z + _angularVelocity.Z * dt);

        var noisyAccel = new Vector3(
            _acceleration.X + _accelDrift.X + _config.AccelerometerNoise * GaussianRandom(),
            _acceleration.Y + _accelDrift.Y + _config.AccelerometerNoise * GaussianRandom(),
            _acceleration.Z + _accelDrift.Z + _config.AccelerometerNoise * GaussianRandom());

        var noisyAngVel = new Vector3(
            _angularVelocity.X + _gyroDrift.X + _config.GyroscopeNoise * GaussianRandom(),
            _angularVelocity.Y + _gyroDrift.Y + _config.GyroscopeNoise * GaussianRandom(),
            _angularVelocity.Z + _gyroDrift.Z + _config.GyroscopeNoise * GaussianRandom());

        LatestReading = new ImuData(_orientation, noisyAngVel, noisyAccel, _currentTime);
    }

    /// <summary>
    /// Sets the angular velocity of the simulated IMU (for external control).
    /// </summary>
    /// <param name="angularVelocity">Angular velocity in radians per second.</param>
    public void SetAngularVelocity(Vector3 angularVelocity) => _angularVelocity = angularVelocity;

    /// <summary>
    /// Sets the linear acceleration of the simulated IMU (for external control).
    /// </summary>
    /// <param name="acceleration">Acceleration in meters per second squared.</param>
    public void SetAcceleration(Vector3 acceleration) => _acceleration = acceleration;

    /// <inheritdoc/>
    public ImuData Read() => LatestReading;

    /// <inheritdoc/>
    public void Dispose() { }

    private double GaussianRandom()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}
