using System;

namespace HelixForge;

/// <summary>
/// Represents a motor or ESC actuator that accepts throttle commands.
/// </summary>
public interface IMotorDevice : IActuator
{
    /// <summary>
    /// Gets the current throttle setting (0.0 to 1.0).
    /// </summary>
    double Throttle { get; }

    /// <summary>
    /// Sets the throttle level for this motor.
    /// </summary>
    /// <param name="normalizedThrottle">Throttle value clamped to [0.0, 1.0].</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="normalizedThrottle"/> is outside [0.0, 1.0].
    /// </exception>
    void SetThrottle(double normalizedThrottle);
}
