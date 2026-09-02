namespace HelixForge;

/// <summary>
/// Represents a device that draws current from a power source, allowing a simulated
/// battery to model sag and capacity drain from real load.
/// </summary>
public interface ICurrentConsumer
{
    /// <summary>
    /// Gets the current draw of this device in amperes.
    /// </summary>
    double Current { get; }
}
