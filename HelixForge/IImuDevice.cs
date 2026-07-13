namespace HelixForge;

/// <summary>
/// Represents an Inertial Measurement Unit providing orientation,
/// angular velocity, and linear acceleration data.
/// </summary>
public interface IImuDevice : ISensor
{
    /// <summary>
    /// Gets the most recent IMU reading.
    /// </summary>
    ImuData LatestReading { get; }

    /// <summary>
    /// Reads the current orientation, angular velocity, and acceleration from the IMU.
    /// </summary>
    /// <returns>A structured IMU data snapshot.</returns>
    ImuData Read();
}
