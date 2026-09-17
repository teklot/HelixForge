namespace HelixForge.Control;

/// <summary>
/// Configuration for a <see cref="ComplementaryFilter"/>.
/// </summary>
public sealed class ComplementaryFilterConfig
{
    /// <summary>
    /// Blending factor between gyroscope integration and accelerometer/magnetometer correction,
    /// in the range [0, 1]. 1.0 trusts the gyroscope exclusively; lower values trust the
    /// accelerometer more. A typical value is 0.98.
    /// </summary>
    public double Alpha { get; set; } = 0.98;

    /// <summary>Initial orientation as Euler angles (roll, pitch, yaw) in radians.</summary>
    public Vector3 InitialOrientation { get; set; } = Vector3.Zero;
}