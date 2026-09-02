using System;

namespace HelixForge;

/// <summary>
/// Represents a differential-drive vehicle that tracks an estimated pose from
/// left/right wheel speeds.
/// </summary>
public interface IDifferentialDriveDevice : IDevice
{
    /// <summary>
    /// Gets the most recent odometry reading.
    /// </summary>
    DifferentialDriveData LatestReading { get; }

    /// <summary>
    /// Gets the target speed of the left wheel in meters per second.
    /// </summary>
    double LeftSpeed { get; }

    /// <summary>
    /// Gets the target speed of the right wheel in meters per second.
    /// </summary>
    double RightSpeed { get; }

    /// <summary>
    /// Sets the target wheel speeds of the drive.
    /// </summary>
    /// <param name="leftSpeed">Target left wheel speed in meters per second.</param>
    /// <param name="rightSpeed">Target right wheel speed in meters per second.</param>
    void SetTargetSpeeds(double leftSpeed, double rightSpeed);

    /// <summary>
    /// Reads the current odometry.
    /// </summary>
    /// <returns>A differential-drive odometry snapshot.</returns>
    DifferentialDriveData Read();
}
