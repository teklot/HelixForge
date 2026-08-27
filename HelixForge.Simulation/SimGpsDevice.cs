using System;

namespace HelixForge.Simulation;

/// <summary>
/// Simulated GPS device with configurable position noise and signal-loss (dropout) behavior.
/// </summary>
public sealed class SimGpsDevice : IGpsDevice
{
    private readonly GpsSimConfig _config;
    private readonly Random _random;
    private double _latitude;
    private double _longitude;
    private double _altitude;
    private Vector3 _velocity;
    private TimeSpan _currentTime;
    private bool _initialized;

    private double _dropoutRemaining;
    private GpsFixStatus _fixStatus;

    /// <summary>
    /// Creates a new simulated GPS device.
    /// </summary>
    /// <param name="deviceId">Unique identifier for this device.</param>
    /// <param name="config">Simulation configuration for GPS behavior.</param>
    /// <param name="random">Random number generator for noise. If null, creates one from config seed.</param>
    public SimGpsDevice(string deviceId, GpsSimConfig config, Random? random = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _random = random ?? new Random(config.RandomSeed ?? Environment.TickCount);
        _latitude = config.InitialLatitude;
        _longitude = config.InitialLongitude;
        _altitude = config.InitialAltitude;
        _velocity = Vector3.Zero;
        _fixStatus = config.BaseFixStatus;
        _dropoutRemaining = 0.0;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public GpsData LatestReading { get; private set; } = GpsData.Empty;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        _initialized = true;
        _currentTime = TimeSpan.Zero;
        _fixStatus = _config.BaseFixStatus;
        _dropoutRemaining = 0.0;
        LatestReading = new GpsData(_latitude, _longitude, _altitude, _velocity, _currentTime, _fixStatus);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _latitude = _config.InitialLatitude;
        _longitude = _config.InitialLongitude;
        _altitude = _config.InitialAltitude;
        _velocity = Vector3.Zero;
        _currentTime = TimeSpan.Zero;
        _fixStatus = _config.BaseFixStatus;
        _dropoutRemaining = 0.0;
        LatestReading = new GpsData(_latitude, _longitude, _altitude, _velocity, _currentTime, _fixStatus);
    }

    /// <inheritdoc/>
    public void Update(TimeSpan deltaTime)
    {
        if (!_initialized)
            return;

        _currentTime += deltaTime;
        AdvanceDropout(deltaTime);

        double dt = deltaTime.TotalSeconds;
        double metersPerDegreeLat = 111320.0;
        double metersPerDegreeLon = 111320.0 * Math.Cos(_latitude * Math.PI / 180.0);

        _latitude += (_velocity.X * dt) / metersPerDegreeLat;
        _longitude += (_velocity.Y * dt) / metersPerDegreeLon;
        _altitude += _velocity.Z * dt;

        double effectiveNoise = _dropoutRemaining > 0.0
            ? _config.UncertaintyDuringDropout
            : _config.PositionNoise;

        double noiseLat = effectiveNoise * GaussianRandom() / metersPerDegreeLat;
        double noiseLon = effectiveNoise * GaussianRandom() / metersPerDegreeLon;
        double noiseAlt = effectiveNoise * GaussianRandom();

        LatestReading = new GpsData(
            _latitude + noiseLat,
            _longitude + noiseLon,
            _altitude + noiseAlt,
            _velocity,
            _currentTime,
            _fixStatus);
    }

    /// <summary>
    /// Sets the velocity of the simulated GPS (for external control).
    /// </summary>
    /// <param name="velocity">Velocity in meters per second (North, East, Down).</param>
    public void SetVelocity(Vector3 velocity) => _velocity = velocity;

    /// <inheritdoc/>
    public GpsData Read() => LatestReading;

    /// <inheritdoc/>
    public void Dispose() { }

    private void AdvanceDropout(TimeSpan deltaTime)
    {
        if (_dropoutRemaining > 0.0)
        {
            _dropoutRemaining -= deltaTime.TotalSeconds;
            _fixStatus = GpsFixStatus.NoFix;
            if (_dropoutRemaining <= 0.0)
            {
                _dropoutRemaining = 0.0;
                _fixStatus = _config.BaseFixStatus;
            }

            return;
        }

        // Not in a dropout window. Enter one probabilistically based on elapsed time.
        double dt = deltaTime.TotalSeconds;
        double p = 1.0 - Math.Exp(-_config.DropoutRate * dt);
        if (_config.DropoutRate > 0.0 && _random.NextDouble() < p)
        {
            _dropoutRemaining = _config.MaxDropoutDuration * (0.5 + 0.5 * _random.NextDouble());
            _fixStatus = GpsFixStatus.NoFix;
        }
        else
        {
            _fixStatus = _config.BaseFixStatus;
        }
    }

    private double GaussianRandom()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}
