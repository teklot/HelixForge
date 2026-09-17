using System;

namespace HelixForge.Control;

/// <summary>
/// Madgwick's gradient-descent attitude filter. Fuses the gyroscope with accelerometer
/// (and optionally magnetometer) corrections to produce a drift-free orientation estimate.
/// </summary>
public sealed class MadgwickFilter : IAttitudeEstimator
{
    private readonly MadgwickFilterConfig _config;
    private Quaternion _quaternion;

    /// <summary>
    /// Creates a new Madgwick filter from the given configuration.
    /// </summary>
    /// <param name="config">Filter configuration.</param>
    public MadgwickFilter(MadgwickFilterConfig config)
    {
        _config = config ?? throw new System.ArgumentNullException(nameof(config));
        _quaternion = config.InitialQuaternion.Normalized;
    }

    /// <summary>
    /// Gets the current orientation as a quaternion.
    /// </summary>
    public Quaternion Quaternion => _quaternion;

    /// <inheritdoc/>
    public Vector3 Orientation => _quaternion.ToEuler();

    /// <inheritdoc/>
    public void Update(TimeSpan dt, Vector3 gyroRadPerSec, Vector3 accelMps2)
    {
        UpdateCore(dt, gyroRadPerSec, accelMps2, null);
    }

    /// <inheritdoc/>
    public void Update(TimeSpan dt, Vector3 gyroRadPerSec, Vector3 accelMps2, Vector3 magTesla)
    {
        UpdateCore(dt, gyroRadPerSec, accelMps2, magTesla);
    }

    private void UpdateCore(TimeSpan dt, Vector3 gyro, Vector3 accel, Vector3? mag)
    {
        double dtSeconds = dt.TotalSeconds;
        if (dtSeconds <= 0.0)
            return;

        double w = _quaternion.W;
        double x = _quaternion.X;
        double y = _quaternion.Y;
        double z = _quaternion.Z;

        // Rate of change of quaternion from the gyroscope.
        double qDotW = 0.5 * (-x * gyro.X - y * gyro.Y - z * gyro.Z);
        double qDotX = 0.5 * (w * gyro.X + y * gyro.Z - z * gyro.Y);
        double qDotY = 0.5 * (w * gyro.Y - x * gyro.Z + z * gyro.X);
        double qDotZ = 0.5 * (w * gyro.Z + x * gyro.Y - y * gyro.X);

        double[] f = new double[] { 0, 0, 0, 0, 0, 0 };
        double[] step = new double[4];
        double bx = 0.0;
        double bz = 0.0;
        int rows = 0;

        double accelMag = accel.Magnitude;
        if (accelMag > 1e-9)
        {
            double ax = accel.X / accelMag;
            double ay = accel.Y / accelMag;
            double az = accel.Z / accelMag;

            f[0] = 2.0 * (x * z - w * y) - ax;
            f[1] = 2.0 * (w * x + y * z) - ay;
            f[2] = 2.0 * (0.5 - x * x - y * y) - az;
            rows = 3;

            if (mag.HasValue && mag.Value.Magnitude > 1e-9)
            {
                Vector3 m = mag.Value.Normalized;
                Vector3 h = _quaternion.Conjugate.Rotate(m);
                bx = Math.Sqrt(h.X * h.X + h.Y * h.Y);
                bz = h.Z;

                f[3] = 2.0 * bx * (0.5 - y * y - z * z) + 2.0 * bz * (x * z - w * y) - m.X;
                f[4] = 2.0 * bx * (x * y - w * z) + 2.0 * bz * (w * x + y * z) - m.Y;
                f[5] = 2.0 * bx * (w * y + x * z) + 2.0 * bz * (0.5 - x * x - y * y) - m.Z;
                rows = 6;
            }

            step[0] = ComputeStep0(f, w, x, y, z, bx, bz, rows);
            step[1] = ComputeStep1(f, w, x, y, z, bx, bz, rows);
            step[2] = ComputeStep2(f, w, x, y, z, bx, bz, rows);
            step[3] = ComputeStep3(f, w, x, y, z, bx, bz, rows);

            double stepMag = Math.Sqrt(step[0] * step[0] + step[1] * step[1] + step[2] * step[2] + step[3] * step[3]);
            if (stepMag > 1e-12)
            {
                double inv = 1.0 / stepMag;
                step[0] *= inv;
                step[1] *= inv;
                step[2] *= inv;
                step[3] *= inv;
            }

            // Apply the gradient-descent feedback before integrating the gyroscope.
            qDotW -= _config.Beta * step[0];
            qDotX -= _config.Beta * step[1];
            qDotY -= _config.Beta * step[2];
            qDotZ -= _config.Beta * step[3];
        }

        _quaternion = new Quaternion(
            w + qDotW * dtSeconds,
            x + qDotX * dtSeconds,
            y + qDotY * dtSeconds,
            z + qDotZ * dtSeconds).Normalized;
    }

    private static double ComputeStep0(double[] f, double w, double x, double y, double z, double bx, double bz, int rows)
    {
        double s = (-2.0 * y) * f[0] + (2.0 * x) * f[1];
        if (rows == 6)
        {
            s += (-2.0 * bz * y) * f[3]
                + (-2.0 * bx * z + 2.0 * bz * x) * f[4]
                + (2.0 * bx * y) * f[5];
        }
        return s;
    }

    private static double ComputeStep1(double[] f, double w, double x, double y, double z, double bx, double bz, int rows)
    {
        double s = (2.0 * z) * f[0] + (2.0 * w) * f[1] + (-4.0 * x) * f[2];
        if (rows == 6)
        {
            s += (2.0 * bz * z) * f[3]
                + (2.0 * bx * y + 2.0 * bz * w) * f[4]
                + (2.0 * bx * z - 4.0 * bz * x) * f[5];
        }
        return s;
    }

    private static double ComputeStep2(double[] f, double w, double x, double y, double z, double bx, double bz, int rows)
    {
        double s = (-2.0 * w) * f[0] + (2.0 * z) * f[1] + (-4.0 * y) * f[2];
        if (rows == 6)
        {
            s += (-4.0 * bx * y - 2.0 * bz * w) * f[3]
                + (2.0 * bx * x + 2.0 * bz * z) * f[4]
                + (2.0 * bx * w - 4.0 * bz * y) * f[5];
        }
        return s;
    }

    private static double ComputeStep3(double[] f, double w, double x, double y, double z, double bx, double bz, int rows)
    {
        double s = (2.0 * x) * f[0] + (2.0 * y) * f[1];
        if (rows == 6)
        {
            s += (-4.0 * bx * z + 2.0 * bz * x) * f[3]
                + (-2.0 * bx * w + 2.0 * bz * y) * f[4]
                + (2.0 * bx * x) * f[5];
        }
        return s;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _quaternion = _config.InitialQuaternion.Normalized;
    }
}