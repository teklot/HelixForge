namespace HelixForge;

/// <summary>
/// Represents a battery providing terminal voltage, current draw, and state of charge.
/// </summary>
public interface IBatteryDevice : ISensor
{
    /// <summary>
    /// Gets the most recent battery reading.
    /// </summary>
    BatteryData LatestReading { get; }

    /// <summary>
    /// Reads the current battery state.
    /// </summary>
    /// <returns>A structured battery data snapshot.</returns>
    BatteryData Read();
}
