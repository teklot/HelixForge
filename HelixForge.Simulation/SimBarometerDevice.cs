using System;

namespace HelixForge.Simulation;

/// <summary>
/// Simulated barometer device with configurable altitude noise, drift, and barometric altitude derivation.
/// </summary>
public sealed class SimBarometerDevice : IBarometerDevice
{
    private readonly BarometerSimConfig _config;
    private readonly Random _random;
    private double _altitude;
    private double _drift;
    private TimeSpan _currentTime;
    private bool _initialized;

    /// <summary>
    /// Creates a new simulated barometer device.
    /// </summary>
    /// <param name="deviceId">Unique identifier for this device.</param>
    /// <param name="config">Simulation configuration for barometer behavior.</param>
    /// <param name="random">Random number generator for noise. If null, creates one from config seed.</param>
    public SimBarometerDevice(string deviceId, BarometerSimConfig config, Random? random = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _random = random ?? new Random(config.RandomSeed ?? Environment.TickCount);
        _altitude = config.InitialAltitude;
        _drift = 0.0;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public BarometerData LatestReading { get; private set; } = BarometerData.Empty;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        _initialized = true;
        _currentTime = TimeSpan.Zero;
        _drift = 0.0;
        LatestReading = new BarometerData(ComputePressure(_altitude, _drift), ComputeAltitude(_altitude, _drift), _currentTime);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _altitude = _config.InitialAltitude;
        _drift = 0.0;
        _currentTime = TimeSpan.Zero;
        LatestReading = new BarometerData(ComputePressure(_altitude, _drift), ComputeAltitude(_altitude, _drift), _currentTime);
    }

    /// <inheritdoc/>
    public void Update(TimeSpan deltaTime)
    {
        if (!_initialized)
            return;

        _currentTime += deltaTime;
        _drift += _config.DriftRate * deltaTime.TotalSeconds * GaussianRandom();

        double noiseMeters = _config.AltitudeNoise * GaussianRandom();
        double noisyAltitude = _altitude + noiseMeters;

        double pressure = ComputePressure(noisyAltitude, _drift);
        double altitude = ComputeAltitude(noisyAltitude, _drift);

        LatestReading = new BarometerData(pressure, altitude, _currentTime);
    }

    /// <summary>
    /// Sets the altitude of the simulated barometer (for external control), in meters.
    /// </summary>
    /// <param name="altitude">Barometric altitude in meters.</param>
    public void SetAltitude(double altitude) => _altitude = altitude;

    /// <inheritdoc/>
    public BarometerData Read() => LatestReading;

    /// <inheritdoc/>
    public void Dispose() { }

    private double ComputePressure(double altitude, double drift) =>
        _config.SeaLevelPressure * Math.Pow(1.0 - 2.25577e-5 * altitude, 5.25888) - drift;

    private double ComputeAltitude(double altitude, double drift) =>
        altitude - drift * 8.43;

    private double GaussianRandom()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}
