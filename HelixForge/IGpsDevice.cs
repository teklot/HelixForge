namespace HelixForge;

/// <summary>
/// Represents a GPS receiver providing position and velocity data.
/// </summary>
public interface IGpsDevice : ISensor
{
    /// <summary>
    /// Gets the most recent GPS reading.
    /// </summary>
    GpsData LatestReading { get; }

    /// <summary>
    /// Reads the current position and velocity from the GPS.
    /// </summary>
    /// <returns>A structured GPS data snapshot.</returns>
    GpsData Read();
}
