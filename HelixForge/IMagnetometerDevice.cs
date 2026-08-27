namespace HelixForge;

/// <summary>
/// Represents a magnetometer providing magnetic field measurements.
/// </summary>
public interface IMagnetometerDevice : ISensor
{
    /// <summary>
    /// Gets the most recent magnetometer reading.
    /// </summary>
    MagData LatestReading { get; }

    /// <summary>
    /// Reads the current magnetic field from the magnetometer.
    /// </summary>
    /// <returns>A structured magnetometer data snapshot.</returns>
    MagData Read();
}
