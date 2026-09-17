using System;

namespace HelixForge.Control;

/// <summary>
/// Fuses gyroscope, accelerometer, and optional magnetometer measurements into an
/// orientation estimate. Implementations must be deterministic for identical input sequences.
/// </summary>
public interface IAttitudeEstimator
{
    /// <summary>
    /// Current orientation estimate as Euler angles in radians: X=roll, Y=pitch, Z=yaw.
    /// </summary>
    Vector3 Orientation { get; }

    /// <summary>
    /// Advances the estimate using gyroscope and accelerometer data only.
    /// </summary>
    /// <param name="dt">Elapsed time since the previous update.</param>
    /// <param name="gyroRadPerSec">Angular velocity from the gyroscope, in radians per second.</param>
    /// <param name="accelMps2">Specific force (acceleration) from the accelerometer, in m/s².</param>
    void Update(TimeSpan dt, Vector3 gyroRadPerSec, Vector3 accelMps2);

    /// <summary>
    /// Advances the estimate using gyroscope, accelerometer, and magnetometer data.
    /// </summary>
    /// <param name="dt">Elapsed time since the previous update.</param>
    /// <param name="gyroRadPerSec">Angular velocity from the gyroscope, in radians per second.</param>
    /// <param name="accelMps2">Specific force (acceleration) from the accelerometer, in m/s².</param>
    /// <param name="magTesla">Magnetic field measurement, in tesla.</param>
    void Update(TimeSpan dt, Vector3 gyroRadPerSec, Vector3 accelMps2, Vector3 magTesla);

    /// <summary>
    /// Resets the estimate to the identity orientation.
    /// </summary>
    void Reset();
}