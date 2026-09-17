namespace HelixForge.Control;

/// <summary>
/// Mode used for the derivative term of a <see cref="PidController"/>.
/// </summary>
public enum DerivativeMode
{
    /// <summary>
    /// Derivative is taken from the change in error. Responds to setpoint changes but causes derivative kick.
    /// </summary>
    OnError,

    /// <summary>
    /// Derivative is taken from the change in measurement. Avoids derivative kick when the setpoint changes.
    /// </summary>
    OnMeasurement
}