namespace HelixForge.Control;

/// <summary>
/// Configuration for a <see cref="MadgwickFilter"/>.
/// </summary>
public sealed class MadgwickFilterConfig
{
    /// <summary>
    /// Gain that controls the rate of convergence of the gradient-descent correction. Larger
    /// values converge faster but pass more accelerometer noise through. A typical value is 0.1.
    /// </summary>
    public double Beta { get; set; } = 0.1;

    /// <summary>Initial orientation as a quaternion. Defaults to identity.</summary>
    public Quaternion InitialQuaternion { get; set; } = Quaternion.Identity;
}