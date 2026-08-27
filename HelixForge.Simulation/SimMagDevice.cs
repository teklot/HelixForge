using System;

namespace HelixForge.Simulation;

/// <summary>
/// Simulated magnetometer device with configurable hard/soft-iron distortion, declination, and noise.
/// </summary>
public sealed class SimMagDevice : IMagnetometerDevice
{
    private readonly MagSimConfig _config;
    private readonly Random _random;
    private Vector3 _orientation;
    private TimeSpan _currentTime;
    private bool _initialized;

    /// <summary>
    /// Creates a new simulated magnetometer device.
    /// </summary>
    /// <param name="deviceId">Unique identifier for this device.</param>
    /// <param name="config">Simulation configuration for magnetometer behavior.</param>
    /// <param name="random">Random number generator for noise. If null, creates one from config seed.</param>
    public SimMagDevice(string deviceId, MagSimConfig config, Random? random = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _random = random ?? new Random(config.RandomSeed ?? Environment.TickCount);
        _orientation = Vector3.Zero;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public MagData LatestReading { get; private set; } = MagData.Empty;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        _initialized = true;
        _currentTime = TimeSpan.Zero;
        LatestReading = new MagData(ComputeReading(_orientation), _currentTime);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _orientation = Vector3.Zero;
        _currentTime = TimeSpan.Zero;
        LatestReading = new MagData(ComputeReading(_orientation), _currentTime);
    }

    /// <inheritdoc/>
    public void Update(TimeSpan deltaTime)
    {
        if (!_initialized)
            return;

        _currentTime += deltaTime;
        LatestReading = new MagData(ComputeReading(_orientation), _currentTime);
    }

    /// <summary>
    /// Sets the orientation of the simulated magnetometer (for external control), in radians (roll, pitch, yaw).
    /// </summary>
    /// <param name="orientation">Orientation in radians.</param>
    public void SetOrientation(Vector3 orientation) => _orientation = orientation;

    /// <inheritdoc/>
    public MagData Read() => LatestReading;

    /// <inheritdoc/>
    public void Dispose() { }

    private Vector3 ComputeReading(Vector3 orientation)
    {
        // Apply magnetic declination first as a world-frame rotation (positive = east),
        // rotating the north component toward the east (+Y) axis.
        double declinationRad = _config.DeclinationDeg * Math.PI / 180.0;
        Vector3 worldField = new Vector3(
            _config.EarthField.X * Math.Cos(declinationRad) - _config.EarthField.Y * Math.Sin(declinationRad),
            _config.EarthField.X * Math.Sin(declinationRad) + _config.EarthField.Y * Math.Cos(declinationRad),
            _config.EarthField.Z);

        // Then rotate the true world field into the body frame by the sensor's orientation.
        // A positive yaw spins the body frame counterclockwise, so a fixed world field
        // appears to rotate clockwise in the body frame.
        Vector3 rotated = new Vector3(
            worldField.X * Math.Cos(orientation.Z) + worldField.Y * Math.Sin(orientation.Z),
            -worldField.X * Math.Sin(orientation.Z) + worldField.Y * Math.Cos(orientation.Z),
            worldField.Z);

        var softIron = new Vector3(
            rotated.X * _config.SoftIronScale.X,
            rotated.Y * _config.SoftIronScale.Y,
            rotated.Z * _config.SoftIronScale.Z);

        var distorted = softIron + _config.HardIronOffset;

        return new Vector3(
            distorted.X + _config.MagneticNoise * GaussianRandom(),
            distorted.Y + _config.MagneticNoise * GaussianRandom(),
            distorted.Z + _config.MagneticNoise * GaussianRandom());
    }

    private double GaussianRandom()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}
