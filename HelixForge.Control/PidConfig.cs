namespace HelixForge.Control;

/// <summary>
/// Configuration for a <see cref="PidController"/>.
/// </summary>
public sealed class PidConfig
{
    /// <summary>Proportional gain.</summary>
    public double Kp { get; set; } = 1.0;

    /// <summary>Integral gain.</summary>
    public double Ki { get; set; } = 0.0;

    /// <summary>Derivative gain.</summary>
    public double Kd { get; set; } = 0.0;

    /// <summary>Minimum output value. Defaults to unbounded below.</summary>
    public double OutputMin { get; set; } = double.MinValue;

    /// <summary>Maximum output value. Defaults to unbounded above.</summary>
    public double OutputMax { get; set; } = double.MaxValue;

    /// <summary>
    /// Clamps the accumulated integral term, providing anti-windup. Defaults to unbounded.
    /// </summary>
    public double IntegralLimit { get; set; } = double.MaxValue;

    /// <summary>
    /// Time constant (in seconds) of the first-order low-pass filter applied to the derivative term.
    /// Zero disables filtering.
    /// </summary>
    public double DerivativeTau { get; set; } = 0.0;

    /// <summary>Which signal the derivative term is computed from.</summary>
    public DerivativeMode DerivativeMode { get; set; } = DerivativeMode.OnMeasurement;
}