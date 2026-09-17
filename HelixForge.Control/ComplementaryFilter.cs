using System;

namespace HelixForge.Control;

/// <summary>
/// One-parameter complementary filter for attitude estimation. Integrates the gyroscope
/// and blends the result toward accelerometer-derived roll/pitch (and optionally a
/// tilt-compensated magnetometer-derived yaw).
/// </summary>
public sealed class ComplementaryFilter : IAttitudeEstimator
{
    private readonly ComplementaryFilterConfig _config;
    private double _roll;
    private double _pitch;
    private double _yaw;

    /// <summary>
    /// Creates a new complementary filter from the given configuration.
    /// </summary>
    /// <param name="config">Filter configuration.</param>
    public ComplementaryFilter(ComplementaryFilterConfig config)
    {
        _config = config ?? throw new System.ArgumentNullException(nameof(config));
        _roll = config.InitialOrientation.X;
        _pitch = config.InitialOrientation.Y;
        _yaw = config.InitialOrientation.Z;
    }

    /// <inheritdoc/>
    public Vector3 Orientation => new Vector3(_roll, _pitch, _yaw);

    /// <inheritdoc/>
    public void Update(TimeSpan dt, Vector3 gyroRadPerSec, Vector3 accelMps2)
    {
        double dtSeconds = dt.TotalSeconds;
        double alpha = _config.Alpha;

        double magnitude = accelMps2.Magnitude;
        if (magnitude > 1e-6)
        {
            double ax = accelMps2.X / magnitude;
            double ay = accelMps2.Y / magnitude;
            double az = accelMps2.Z / magnitude;

            double rollFromAccel = Math.Atan2(ay, Math.Sqrt(ax * ax + az * az));
            double pitchFromAccel = Math.Atan2(-ax, Math.Sqrt(ay * ay + az * az));

            _roll = alpha * (_roll + gyroRadPerSec.X * dtSeconds) + (1.0 - alpha) * rollFromAccel;
            _pitch = alpha * (_pitch + gyroRadPerSec.Y * dtSeconds) + (1.0 - alpha) * pitchFromAccel;
        }
        else
        {
            _roll += gyroRadPerSec.X * dtSeconds;
            _pitch += gyroRadPerSec.Y * dtSeconds;
        }

        _yaw += gyroRadPerSec.Z * dtSeconds;
    }

    /// <inheritdoc/>
    public void Update(TimeSpan dt, Vector3 gyroRadPerSec, Vector3 accelMps2, Vector3 magTesla)
    {
        Update(dt, gyroRadPerSec, accelMps2);

        double magnitude = magTesla.Magnitude;
        if (magnitude <= 1e-6)
            return;

        double mx = magTesla.X / magnitude;
        double my = magTesla.Y / magnitude;
        double mz = magTesla.Z / magnitude;

        double sinRoll = Math.Sin(_roll);
        double cosRoll = Math.Cos(_roll);
        double sinPitch = Math.Sin(_pitch);
        double cosPitch = Math.Cos(_pitch);

        double xHorizontal = mx * cosPitch + my * sinRoll * sinPitch + mz * cosRoll * sinPitch;
        double yHorizontal = my * cosRoll - mz * sinRoll;
        double yawFromMag = Math.Atan2(-yHorizontal, xHorizontal);

        double alpha = _config.Alpha;
        _yaw = alpha * _yaw + (1.0 - alpha) * yawFromMag;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _roll = _config.InitialOrientation.X;
        _pitch = _config.InitialOrientation.Y;
        _yaw = _config.InitialOrientation.Z;
    }
}