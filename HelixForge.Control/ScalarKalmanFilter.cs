namespace HelixForge.Control;

/// <summary>
/// Configuration for a <see cref="ScalarKalmanFilter"/>.
/// </summary>
public sealed class ScalarKalmanConfig
{
    /// <summary>
    /// Process noise variance (added to the estimate covariance each second of prediction).
    /// Larger values trust measurements more.
    /// </summary>
    public double ProcessNoise { get; set; } = 1.0;

    /// <summary>
    /// Measurement noise variance. Larger values trust the model more than individual measurements.
    /// </summary>
    public double MeasurementNoise { get; set; } = 1.0;

    /// <summary>
    /// Initial estimate before any measurements arrive. Defaults to zero.
    /// </summary>
    public double InitialEstimate { get; set; } = 0.0;

    /// <summary>
    /// Initial estimate covariance. Defaults to a large value so early measurements
    /// dominate before the filter converges.
    /// </summary>
    public double InitialCovariance { get; set; } = 100.0;
}

/// <summary>
/// A deterministic, allocation-free Kalman filter for a single constant state with scalar
/// measurements and a random-walk process model. The natural building block for larger
/// stacked estimators.
/// </summary>
public sealed class ScalarKalmanFilter
{
    private readonly ScalarKalmanConfig _config;
    private double _estimate;
    private double _covariance;

    /// <summary>
    /// Creates a new scalar Kalman filter from the given configuration.
    /// </summary>
    /// <param name="config">Filter configuration.</param>
    public ScalarKalmanFilter(ScalarKalmanConfig config)
    {
        _config = config ?? throw new System.ArgumentNullException(nameof(config));
        _estimate = config.InitialEstimate;
        _covariance = config.InitialCovariance;
    }

    /// <summary>
    /// Gets the current state estimate.
    /// </summary>
    public double Estimate => _estimate;

    /// <summary>
    /// Gets the current estimate covariance (uncertainty).
    /// </summary>
    public double Covariance => _covariance;

    /// <summary>
    /// Advances the estimate covariance by the process noise for the given elapsed time.
    /// </summary>
    /// <param name="dt">Elapsed time, in seconds.</param>
    public void Predict(double dt)
    {
        _covariance += _config.ProcessNoise * dt;
    }

    /// <summary>
    /// Incorporates a scalar measurement into the current estimate.
    /// </summary>
    /// <param name="measurement">The measured value.</param>
    public void Update(double measurement)
    {
        double gain = _covariance / (_covariance + _config.MeasurementNoise);
        _estimate += gain * (measurement - _estimate);
        _covariance *= (1.0 - gain);
    }

    /// <summary>
    /// Performs a predict step followed by an update step with a measurement.
    /// </summary>
    /// <param name="measurement">The measured value.</param>
    /// <param name="dt">Elapsed time since the previous measurement, in seconds.</param>
    public double Update(double measurement, double dt)
    {
        Predict(dt);
        Update(measurement);
        return _estimate;
    }

    /// <summary>
    /// Resets the filter to its configured initial state.
    /// </summary>
    public void Reset()
    {
        _estimate = _config.InitialEstimate;
        _covariance = _config.InitialCovariance;
    }
}