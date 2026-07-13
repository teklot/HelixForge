namespace HelixForge;

/// <summary>
/// Represents an actuator device that receives commands from the control system.
/// </summary>
public interface IActuator : IDevice
{
    /// <summary>
    /// Gets whether the actuator is currently accepting commands.
    /// </summary>
    bool IsActive { get; }
}
