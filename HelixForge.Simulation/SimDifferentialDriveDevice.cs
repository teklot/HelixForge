using System;

namespace HelixForge.Simulation;

/// <summary>
/// Simulated differential-drive vehicle integrating a kinematic odometry pose
/// from configured wheel speeds and track width.
/// </summary>
public sealed class SimDifferentialDriveDevice : IDifferentialDriveDevice
{
    private readonly DifferentialDriveSimConfig _config;
    private readonly Random _random;
    private double _targetLeft;
    private double _targetRight;
    private Pose2D _pose;
    private TimeSpan _currentTime;
    private bool _initialized;

    /// <summary>
    /// Creates a new simulated differential-drive device.
    /// </summary>
    /// <param name="deviceId">Unique identifier for this device.</param>
    /// <param name="config">Simulation configuration for drive dynamics.</param>
    /// <param name="random">Random number generator for noise. If null, creates one from config seed.</param>
    public SimDifferentialDriveDevice(string deviceId, DifferentialDriveSimConfig config, Random? random = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _random = random ?? new Random(config.RandomSeed ?? Environment.TickCount);
        _pose = config.InitialPose;
        _targetLeft = 0.0;
        _targetRight = 0.0;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public DifferentialDriveData LatestReading { get; private set; } = DifferentialDriveData.Empty;

    /// <inheritdoc/>
    public double LeftSpeed => _targetLeft;

    /// <inheritdoc/>
    public double RightSpeed => _targetRight;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        _initialized = true;
        _currentTime = TimeSpan.Zero;
        _pose = _config.InitialPose;
        LatestReading = ComputeReading(_currentTime);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _currentTime = TimeSpan.Zero;
        _pose = _config.InitialPose;
        _targetLeft = 0.0;
        _targetRight = 0.0;
        LatestReading = ComputeReading(_currentTime);
    }

    /// <inheritdoc/>
    public void SetTargetSpeeds(double leftSpeed, double rightSpeed)
    {
        _targetLeft = ClampSpeed(leftSpeed);
        _targetRight = ClampSpeed(rightSpeed);
    }

    /// <summary>
    /// Advances the odometry pose by the configured wheel speeds.
    /// </summary>
    /// <param name="deltaTime">The elapsed time since the last update.</param>
    public void Update(TimeSpan deltaTime)
    {
        if (!_initialized)
            return;

        _currentTime += deltaTime;
        double dt = deltaTime.TotalSeconds;

        double v = (_targetLeft + _targetRight) / 2.0;
        double omega = (_targetRight - _targetLeft) / _config.TrackWidth;

        double yaw = _pose.Yaw + omega * dt;
        double x = _pose.X + v * Math.Cos(_pose.Yaw) * dt;
        double y = _pose.Y + v * Math.Sin(_pose.Yaw) * dt;

        if (_config.OdometryNoise > 0.0)
        {
            x += _config.OdometryNoise * GaussianRandom();
            y += _config.OdometryNoise * GaussianRandom();
        }

        _pose = new Pose2D(x, y, yaw);
        LatestReading = ComputeReading(_currentTime);
    }

    /// <inheritdoc/>
    public DifferentialDriveData Read() => LatestReading;

    /// <inheritdoc/>
    public void Dispose() { }

    private DifferentialDriveData ComputeReading(TimeSpan timestamp)
    {
        double v = (_targetLeft + _targetRight) / 2.0;
        double omega = (_targetRight - _targetLeft) / _config.TrackWidth;
        return new DifferentialDriveData(_pose, v, omega, timestamp);
    }

    private double ClampSpeed(double value)
    {
        double max = _config.MaxSpeed;
        return value < -max ? -max : (value > max ? max : value);
    }

    private double GaussianRandom()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}
