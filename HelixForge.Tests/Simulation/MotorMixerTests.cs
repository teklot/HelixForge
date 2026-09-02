using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class MotorMixerTests
{
    [Fact]
    public void MixQuadX_Hover_EqualThrottles()
    {
        var result = MotorMixer.MixQuadX(new MixerCommand(0.5, 0, 0, 0));

        Assert.Equal(0.5, result.FrontLeft, 6);
        Assert.Equal(0.5, result.FrontRight, 6);
        Assert.Equal(0.5, result.BackLeft, 6);
        Assert.Equal(0.5, result.BackRight, 6);
    }

    [Fact]
    public void MixQuadX_Roll_DifferentiatesLeftRight()
    {
        var result = MotorMixer.MixQuadX(new MixerCommand(0.5, 0.2, 0, 0));

        // Roll + raises left motors, lowers right motors.
        Assert.Equal(0.7, result.FrontLeft, 6);
        Assert.Equal(0.7, result.BackLeft, 6);
        Assert.Equal(0.3, result.FrontRight, 6);
        Assert.Equal(0.3, result.BackRight, 6);
    }

    [Fact]
    public void MixQuadX_Pitch_DifferentiatesFrontBack()
    {
        var result = MotorMixer.MixQuadX(new MixerCommand(0.5, 0, 0.2, 0));

        Assert.Equal(0.7, result.FrontLeft, 6);
        Assert.Equal(0.7, result.FrontRight, 6);
        Assert.Equal(0.3, result.BackLeft, 6);
        Assert.Equal(0.3, result.BackRight, 6);
    }

    [Fact]
    public void MixQuadX_ClampsToUnitInterval()
    {
        var result = MotorMixer.MixQuadX(new MixerCommand(0.9, 0.9, 0.9, 0.9));

        Assert.True(result.FrontLeft <= 1.0);
        Assert.True(result.FrontRight <= 1.0);
        Assert.True(result.BackLeft <= 1.0);
        Assert.True(result.BackRight <= 1.0);
        Assert.True(result.FrontLeft >= 0.0);
    }
}
