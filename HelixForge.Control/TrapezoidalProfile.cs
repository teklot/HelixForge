namespace HelixForge.Control;

/// <summary>
/// The evaluated state of a <see cref="TrapezoidalProfile"/> at a point in time.
/// </summary>
public readonly struct TrapezoidalProfileState
{
    /// <summary>Position along the profile, in units.</summary>
    public double Position { get; }

    /// <summary>Velocity, in units per second.</summary>
    public double Velocity { get; }

    /// <summary>Acceleration, in units per second squared.</summary>
    public double Acceleration { get; }

    /// <summary>Whether the profile has reached its target and the motion is complete.</summary>
    public bool IsComplete { get; }

    /// <summary>
    /// Creates a new profile state.
    /// </summary>
    public TrapezoidalProfileState(double position, double velocity, double acceleration, bool isComplete)
    {
        Position = position;
        Velocity = velocity;
        Acceleration = acceleration;
        IsComplete = isComplete;
    }
}

/// <summary>
/// A deterministic, allocation-free trapezoidal (accel-cruise-decel) trajectory profile.
/// Splits motion into three phases: constant acceleration, constant velocity, and constant
/// deceleration, folding to a triangular profile when the move is too short to reach max velocity.
/// </summary>
public sealed class TrapezoidalProfile
{
    private readonly TrapezoidalProfileConfig _config;
    private double _start;
    private double _target;
    private double _direction;
    private double _distance;
    private double _t1;
    private double _t2;
    private double _t3;
    private double _cruiseVelocity;
    private double _acceleration;
    private double _deceleration;

    /// <summary>
    /// Creates a new trapezoidal profile from the given configuration.
    /// </summary>
    /// <param name="config">Profile configuration.</param>
    public TrapezoidalProfile(TrapezoidalProfileConfig config)
    {
        _config = config ?? throw new System.ArgumentNullException(nameof(config));
        Reset();
    }

    /// <summary>
    /// Gets the total planned duration of the current profile, in seconds. Zero when idle.
    /// </summary>
    public double TotalDuration => _t3;

    /// <summary>
    /// Gets the start position of the current profile.
    /// </summary>
    public double Start => _start;

    /// <summary>
    /// Gets the target position of the current profile.
    /// </summary>
    public double Target => _target;

    /// <summary>
    /// Arms the profile to move from <paramref name="start"/> to <paramref name="target"/>.
    /// </summary>
    /// <param name="start">Starting position.</param>
    /// <param name="target">Target position.</param>
    public void SetTarget(double start, double target)
    {
        _start = start;
        _target = target;
        _distance = Math.Abs(target - start);
        _direction = target >= start ? 1.0 : -1.0;

        if (_distance < 1e-12)
        {
            _cruiseVelocity = 0.0;
            _acceleration = 0.0;
            _deceleration = 0.0;
            _t1 = _t2 = _t3 = 0.0;
            return;
        }

        double maxVelocity = Math.Max(0.0, _config.MaxVelocity);
        _acceleration = Math.Max(0.0, _config.MaxAcceleration);
        _deceleration = Math.Max(0.0, _config.MaxDeceleration > 0.0 ? _config.MaxDeceleration : _acceleration);

        // Time and distance to reach max velocity.
        double tRamp = maxVelocity / _acceleration;
        double dRamp = 0.5 * _acceleration * tRamp * tRamp;
        double tDecel = maxVelocity / _deceleration;
        double dDecel = 0.5 * _deceleration * tDecel * tDecel;

        if (dRamp + dDecel <= _distance)
        {
            // Full trapezoid.
            _cruiseVelocity = maxVelocity;
            double dCruise = _distance - dRamp - dDecel;
            double tCruise = dCruise / maxVelocity;
            _t1 = tRamp;
            _t2 = tRamp + tCruise;
            _t3 = _t2 + tDecel;
        }
        else
        {
            // Short move: triangle profile reaching a peak below max velocity.
            double peak = Math.Sqrt(
                (2.0 * _distance * _acceleration * _deceleration) /
                (_acceleration + _deceleration));
            _cruiseVelocity = peak;
            double tRampPeak = peak / _acceleration;
            double tDecelPeak = peak / _deceleration;
            _t1 = tRampPeak;
            _t2 = tRampPeak;
            _t3 = tRampPeak + tDecelPeak;
        }
    }

    /// <summary>
    /// Evaluates the profile at the given elapsed time since <see cref="SetTarget"/>
    /// was called.
    /// </summary>
    /// <param name="elapsed">Elapsed time.</param>
    /// <returns>The trajectory state at that time.</returns>
    public TrapezoidalProfileState Evaluate(TimeSpan elapsed)
    {
        return Evaluate(elapsed.TotalSeconds);
    }

    /// <summary>
    /// Evaluates the profile at the given elapsed time, in seconds, since
    /// <see cref="SetTarget"/> was called.
    /// </summary>
    /// <param name="elapsedSeconds">Elapsed time, in seconds.</param>
    /// <returns>The trajectory state at that time.</returns>
    public TrapezoidalProfileState Evaluate(double elapsedSeconds)
    {
        if (_distance < 1e-12)
            return new TrapezoidalProfileState(_target, 0.0, 0.0, true);

        double position;
        double velocity;
        double acceleration;

        if (elapsedSeconds < _t1)
        {
            // Ramp up.
            position = 0.5 * _acceleration * elapsedSeconds * elapsedSeconds;
            velocity = _acceleration * elapsedSeconds;
            acceleration = _acceleration;
        }
        else if (elapsedSeconds < _t2)
        {
            // Cruise.
            double t = elapsedSeconds - _t1;
            position = 0.5 * _acceleration * _t1 * _t1 + _cruiseVelocity * t;
            velocity = _cruiseVelocity;
            acceleration = 0.0;
        }
        else if (elapsedSeconds < _t3)
        {
            // Ramp down: remaining distance shrinks with the square of remaining time.
            double remaining = _t3 - elapsedSeconds;
            position = _distance - 0.5 * _deceleration * remaining * remaining;
            velocity = _deceleration * remaining;
            acceleration = -_deceleration;
        }
        else
        {
            position = _distance;
            velocity = 0.0;
            acceleration = 0.0;
        }

        bool isComplete = elapsedSeconds >= _t3;
        return new TrapezoidalProfileState(
            _start + _direction * position,
            _direction * velocity,
            _direction * acceleration,
            isComplete);
    }

    /// <summary>
    /// Clears the profile back to idle and starting position <paramref name="position"/>.
    /// </summary>
    /// <param name="position">The position to hold.</param>
    public void Reset(double position)
    {
        _start = position;
        _target = position;
        _distance = 0.0;
        _direction = 1.0;
        _t1 = _t2 = _t3 = 0.0;
        _cruiseVelocity = 0.0;
        _acceleration = 0.0;
        _deceleration = 0.0;
    }

    /// <summary>
    /// Clears the profile back to idle at position zero.
    /// </summary>
    public void Reset()
    {
        Reset(0.0);
    }
}