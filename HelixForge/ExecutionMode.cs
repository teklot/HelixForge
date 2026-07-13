namespace HelixForge;

/// <summary>
/// Determines the execution backend for device implementations.
/// Mode is selected at system composition time via configuration;
/// devices never reference this enum internally.
/// </summary>
public enum ExecutionMode
{
    /// <summary>Simulation backend. Devices use software behavior models.</summary>
    Simulation = 0,

    /// <summary>Real hardware drivers. Devices talk to physical hardware.</summary>
    Real = 1
}
