namespace HelixForge.Control;

/// <summary>
/// Configuration for a <see cref="TrapezoidalProfile"/>.
/// </summary>
public sealed class TrapezoidalProfileConfig
{
    /// <summary>Maximum velocity, in units per second.</summary>
    public double MaxVelocity { get; set; } = 1.0;

    /// <summary>Maximum acceleration during the ramp-up phase, in units per second squared.</summary>
    public double MaxAcceleration { get; set; } = 1.0;

    /// <summary>
    /// Maximum deceleration during the ramp-down phase, in units per second squared.
    /// When zero, falls back to <see cref="MaxAcceleration"/>.
    /// </summary>
    public double MaxDeceleration { get; set; } = 0.0;
}