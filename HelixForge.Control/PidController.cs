namespace HelixForge.Control;

/// <summary>
/// Deterministic, allocation-free PID controller with output clamps, clamped-integral
/// anti-windup, and an optional filtered derivative.
/// </summary>
public sealed class PidController
{
    private readonly PidConfig _config;
    private double _integral;
    private double _previousError;
    private double _previousMeasurement;
    private double _filteredDerivative;
    private bool _hasPreviousInput;

    /// <summary>
    /// Creates a new PID controller from the given configuration.
    /// </summary>
    /// <param name="config">Controller configuration.</param>
    public PidController(PidConfig config)
    {
        _config = config ?? throw new System.ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Gets the controller configuration.
    /// </summary>
    public PidConfig Config => _config;

    /// <summary>
    /// Gets the current accumulated integral term.
    /// </summary>
    public double Integral => _integral;

    /// <summary>
    /// Gets the output of the most recent <see cref="Step"/> call.
    /// </summary>
    public double Output { get; private set; }

    /// <summary>
    /// Advances the controller one time step.
    /// </summary>
    /// <param name="setpoint">Desired value.</param>
    /// <param name="measurement">Current measured value.</param>
    /// <param name="dt">Elapsed time since the previous step, in seconds.</param>
    /// <returns>The control output, clamped to <see cref="PidConfig.OutputMin"/> and <see cref="PidConfig.OutputMax"/>.</returns>
    public double Step(double setpoint, double measurement, double dt)
    {
        if (dt <= 0.0)
            throw new System.ArgumentOutOfRangeException(nameof(dt), dt, "Time step must be positive.");

        double error = setpoint - measurement;

        double derivative;
        if (!_hasPreviousInput)
        {
            derivative = 0.0;
            _hasPreviousInput = true;
        }
        else if (_config.DerivativeMode == DerivativeMode.OnError)
        {
            derivative = (error - _previousError) / dt;
        }
        else
        {
            derivative = -(measurement - _previousMeasurement) / dt;
        }

        if (_config.DerivativeTau > 0.0)
        {
            double alpha = dt / (_config.DerivativeTau + dt);
            _filteredDerivative += alpha * (derivative - _filteredDerivative);
            derivative = _filteredDerivative;
        }

        _integral += error * dt;
        if (_integral > _config.IntegralLimit)
            _integral = _config.IntegralLimit;
        else if (_integral < -_config.IntegralLimit)
            _integral = -_config.IntegralLimit;

        Output = Clamp(
            _config.Kp * error + _config.Ki * _integral + _config.Kd * derivative,
            _config.OutputMin,
            _config.OutputMax);

        _previousError = error;
        _previousMeasurement = measurement;
        return Output;
    }

    /// <summary>
    /// Clears integral and derivative state. Call between runs or when the controlled system restarts.
    /// </summary>
    public void Reset()
    {
        _integral = 0.0;
        _previousError = 0.0;
        _previousMeasurement = 0.0;
        _filteredDerivative = 0.0;
        _hasPreviousInput = false;
        Output = 0.0;
    }

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : (value > max ? max : value);
}