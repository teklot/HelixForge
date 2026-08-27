namespace HelixForge;

/// <summary>
/// Represents a servo actuator that accepts positional angle commands.
/// </summary>
public interface IServoDevice : IActuator
{
    /// <summary>
    /// Gets the current achieved angle in degrees.
    /// </summary>
    double Angle { get; }

    /// <summary>
    /// Gets the most recently commanded target angle in degrees.
    /// </summary>
    double TargetAngle { get; }

    /// <summary>
    /// Sets the target angle for this servo in degrees.
    /// </summary>
    /// <param name="degrees">Target angle in degrees, clamped to the device's configured limits.</param>
    void SetAngle(double degrees);
}
