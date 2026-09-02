using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class SimDifferentialDriveDeviceTests
{
    [Fact]
    public void Initialize_SetsPoseToConfig()
    {
        var config = new DifferentialDriveSimConfig { InitialPose = new Pose2D(1.0, 2.0, 0.5) };
        var drive = new SimDifferentialDriveDevice("dd-01", config);
        drive.Initialize();

        var data = drive.Read();
        Assert.Equal(1.0, data.Pose.X, 6);
        Assert.Equal(2.0, data.Pose.Y, 6);
        Assert.Equal(0.5, data.Pose.Yaw, 6);
    }

    [Fact]
    public void Update_EqualSpeeds_MovesStraight()
    {
        var config = new DifferentialDriveSimConfig { InitialPose = Pose2D.Zero, MaxSpeed = 1.0 };
        var drive = new SimDifferentialDriveDevice("dd-01", config);
        drive.Initialize();

        drive.SetTargetSpeeds(0.5, 0.5);
        drive.Update(TimeSpan.FromSeconds(1));

        var data = drive.Read();
        // v = (0.5+0.5)/2 = 0.5 m/s for 1s -> 0.5m along heading (yaw=0 => +X).
        Assert.Equal(0.5, data.Pose.X, 4);
        Assert.Equal(0.0, data.Pose.Y, 4);
        Assert.Equal(0.0, data.Pose.Yaw, 4);
        Assert.Equal(0.5, data.LinearSpeed, 6);
        Assert.Equal(0.0, data.AngularSpeed, 6);
    }

    [Fact]
    public void Update_UnequalSpeeds_TurnsInPlace()
    {
        var config = new DifferentialDriveSimConfig { TrackWidth = 0.5 };
        var drive = new SimDifferentialDriveDevice("dd-01", config);
        drive.Initialize();

        drive.SetTargetSpeeds(-0.5, 0.5);
        drive.Update(TimeSpan.FromSeconds(1));

        var data = drive.Read();
        // v = 0, omega = (0.5 - (-0.5)) / 0.5 = 2 rad/s -> yaw = 2 rad.
        Assert.Equal(0.0, data.Pose.X, 4);
        Assert.Equal(0.0, data.Pose.Y, 4);
        Assert.Equal(2.0, data.Pose.Yaw, 4);
        Assert.Equal(2.0, data.AngularSpeed, 6);
    }

    [Fact]
    public void SetTargetSpeeds_ClampsAtMax()
    {
        var config = new DifferentialDriveSimConfig { MaxSpeed = 1.0 };
        var drive = new SimDifferentialDriveDevice("dd-01", config);
        drive.Initialize();

        drive.SetTargetSpeeds(5.0, -5.0);

        Assert.Equal(1.0, drive.LeftSpeed, 6);
        Assert.Equal(-1.0, drive.RightSpeed, 6);
    }

    [Fact]
    public void Reset_ReturnsToInitialPose()
    {
        var config = new DifferentialDriveSimConfig { InitialPose = new Pose2D(3, 4, 1) };
        var drive = new SimDifferentialDriveDevice("dd-01", config);
        drive.Initialize();
        drive.SetTargetSpeeds(1.0, 1.0);
        drive.Update(TimeSpan.FromSeconds(1));
        drive.Update(TimeSpan.FromSeconds(1));

        drive.Reset();

        var data = drive.Read();
        Assert.Equal(3.0, data.Pose.X, 6);
        Assert.Equal(4.0, data.Pose.Y, 6);
        Assert.Equal(1.0, data.Pose.Yaw, 6);
    }
}
