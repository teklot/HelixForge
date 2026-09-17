using HelixForge.Control;

namespace HelixForge.Tests.Control;

public class TrapezoidalProfileTests
{
    [Fact]
    public void FullTrapezoid_ReachesTarget()
    {
        var profile = new TrapezoidalProfile(new TrapezoidalProfileConfig
        {
            MaxVelocity = 1.0,
            MaxAcceleration = 1.0,
            MaxDeceleration = 1.0
        });

        // Ramp 1s (0.5u) + cruise 1s (1u) + decel 1s (0.5u) = 2u total, 3s.
        profile.SetTarget(start: 0.0, target: 2.0);

        Assert.Equal(3.0, profile.TotalDuration, 10);

        var mid = profile.Evaluate(TimeSpan.FromSeconds(1.5));
        Assert.Equal(1.0, mid.Position, 6);
        Assert.Equal(1.0, mid.Velocity, 6);
        Assert.Equal(0.0, mid.Acceleration, 6);
        Assert.False(mid.IsComplete);

        var end = profile.Evaluate(TimeSpan.FromSeconds(3.0));
        Assert.Equal(2.0, end.Position, 6);
        Assert.Equal(0.0, end.Velocity, 6);
        Assert.True(end.IsComplete);
    }

    [Fact]
    public void ShortMove_FoldsToTriangle()
    {
        var profile = new TrapezoidalProfile(new TrapezoidalProfileConfig
        {
            MaxVelocity = 10.0,
            MaxAcceleration = 1.0,
            MaxDeceleration = 1.0
        });

        // Too short to reach max velocity: 1u -> peak 1u/s, ramp 1s, decel 1s, total 2s.
        profile.SetTarget(start: 0.0, target: 1.0);

        Assert.Equal(2.0, profile.TotalDuration, 10);

        var peak = profile.Evaluate(TimeSpan.FromSeconds(1.0));
        Assert.Equal(0.5, peak.Position, 6);
        Assert.Equal(1.0, peak.Velocity, 6);

        var end = profile.Evaluate(TimeSpan.FromSeconds(2.0));
        Assert.Equal(1.0, end.Position, 6);
        Assert.Equal(0.0, end.Velocity, 6);
        Assert.True(end.IsComplete);
    }

    [Fact]
    public void NegativeDirection_MovesBackwards()
    {
        var profile = new TrapezoidalProfile(new TrapezoidalProfileConfig
        {
            MaxVelocity = 2.0,
            MaxAcceleration = 1.0,
            MaxDeceleration = 1.0
        });

        profile.SetTarget(start: 5.0, target: 1.0);

        var state = profile.Evaluate(TimeSpan.FromSeconds(0.5));

        Assert.True(state.Position < 5.0);
        Assert.True(state.Velocity < 0.0);
    }

    [Fact]
    public void OverTime_StaysAtTarget()
    {
        var profile = new TrapezoidalProfile(new TrapezoidalProfileConfig { MaxVelocity = 1.0 });

        profile.SetTarget(start: 0.0, target: 1.0);

        var state = profile.Evaluate(TimeSpan.FromSeconds(999.0));

        Assert.Equal(1.0, state.Position, 6);
        Assert.True(state.IsComplete);
    }

    [Fact]
    public void ZeroDistance_IsImmediatelyComplete()
    {
        var profile = new TrapezoidalProfile(new TrapezoidalProfileConfig { MaxVelocity = 1.0 });

        profile.SetTarget(start: 3.0, target: 3.0);

        var state = profile.Evaluate(TimeSpan.Zero);

        Assert.True(state.IsComplete);
        Assert.Equal(3.0, state.Position, 10);
    }

    [Fact]
    public void Reset_ClearsProfile()
    {
        var profile = new TrapezoidalProfile(new TrapezoidalProfileConfig { MaxVelocity = 1.0 });

        profile.SetTarget(start: 0.0, target: 5.0);
        Assert.True(profile.TotalDuration > 0.0);

        profile.Reset(2.0);

        Assert.Equal(0.0, profile.TotalDuration, 10);
        Assert.Equal(2.0, profile.Start, 10);
        var state = profile.Evaluate(TimeSpan.Zero);
        Assert.True(state.IsComplete);
        Assert.Equal(2.0, state.Position, 10);
    }

    [Fact]
    public void RepeatedRuns_AreDeterministic()
    {
        double RunOnce()
        {
            var profile = new TrapezoidalProfile(new TrapezoidalProfileConfig
            {
                MaxVelocity = 2.0,
                MaxAcceleration = 3.0,
                MaxDeceleration = 4.0
            });
            profile.SetTarget(start: 1.0, target: 7.0);

            TrapezoidalProfileState last = default;
            for (int i = 0; i < 100; i++)
                last = profile.Evaluate(TimeSpan.FromMilliseconds(i * 20));
            return last.Position;
        }

        Assert.Equal(RunOnce(), RunOnce(), 12);
    }
}