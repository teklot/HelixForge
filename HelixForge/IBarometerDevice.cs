namespace HelixForge;

/// <summary>
/// Represents a barometer providing atmospheric pressure and barometric altitude.
/// </summary>
public interface IBarometerDevice : ISensor
{
    /// <summary>
    /// Gets the most recent barometer reading.
    /// </summary>
    BarometerData LatestReading { get; }

    /// <summary>
    /// Reads the current pressure and barometric altitude from the barometer.
    /// </summary>
    /// <returns>A structured barometer data snapshot.</returns>
    BarometerData Read();
}
