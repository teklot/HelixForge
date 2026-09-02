namespace HelixForge.Simulation;

/// <summary>
/// Normalized flight command for a multi-rotor mixer: collective throttle plus
/// pitch/roll/yaw stick inputs, each in [-1.0, 1.0].
/// </summary>
public readonly struct MixerCommand
{
    /// <summary>Collective throttle, typically [0.0, 1.0].</summary>
    public double Throttle { get; }

    /// <summary>Pitch input in [-1.0, 1.0].</summary>
    public double Pitch { get; }

    /// <summary>Roll input in [-1.0, 1.0].</summary>
    public double Roll { get; }

    /// <summary>Yaw input in [-1.0, 1.0].</summary>
    public double Yaw { get; }

    /// <summary>Creates a normalized mixer command.</summary>
    /// <param name="throttle">Collective throttle.</param>
    /// <param name="roll">Roll input.</param>
    /// <param name="pitch">Pitch input.</param>
    /// <param name="yaw">Yaw input.</param>
    public MixerCommand(double throttle, double roll, double pitch, double yaw)
    {
        Throttle = throttle;
        Roll = roll;
        Pitch = pitch;
        Yaw = yaw;
    }
}

/// <summary>
/// Result of a quad-X motor mix: per-corner normalized throttle commands for the
/// four motors (front-left, front-right, back-left, back-right).
/// </summary>
public readonly struct QuadMotors
{
    /// <summary>Front-left throttle in [0.0, 1.0].</summary>
    public double FrontLeft { get; }

    /// <summary>Front-right throttle in [0.0, 1.0].</summary>
    public double FrontRight { get; }

    /// <summary>Back-left throttle in [0.0, 1.0].</summary>
    public double BackLeft { get; }

    /// <summary>Back-right throttle in [0.0, 1.0].</summary>
    public double BackRight { get; }

    /// <summary>Creates a quad motor mix result.</summary>
    public QuadMotors(double frontLeft, double frontRight, double backLeft, double backRight)
    {
        FrontLeft = frontLeft;
        FrontRight = frontRight;
        BackLeft = backLeft;
        BackRight = backRight;
    }
}

/// <summary>
/// Alloc-free helper distributing normalized flight commands to quad-X motor
/// throttles.
/// </summary>
public static class MotorMixer
{
    /// <summary>
    /// Distributes a command across a quad-X motor arrangement, clamped to
    /// [0.0, 1.0]. Adjacent (pitch/roll) and counter-rotating (yaw) pairs are
    /// allocated with the standard X-mix signs.
    /// </summary>
    /// <param name="cmd">The normalized flight command.</param>
    public static QuadMotors MixQuadX(MixerCommand cmd)
    {
        double fl = cmd.Throttle + cmd.Roll + cmd.Pitch - cmd.Yaw;
        double fr = cmd.Throttle - cmd.Roll + cmd.Pitch + cmd.Yaw;
        double bl = cmd.Throttle + cmd.Roll - cmd.Pitch + cmd.Yaw;
        double br = cmd.Throttle - cmd.Roll - cmd.Pitch - cmd.Yaw;

        return new QuadMotors(Clamp01(fl), Clamp01(fr), Clamp01(bl), Clamp01(br));
    }

    private static double Clamp01(double value) => value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
}
