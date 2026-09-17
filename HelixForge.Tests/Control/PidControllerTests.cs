using HelixForge.Control;

namespace HelixForge.Tests.Control;

public class PidControllerTests
{
    [Fact]
    public void Proportional_OutputsGainTimesError()
    {
        var pid = new PidController(new PidConfig { Kp = 2.0 });

        double output = pid.Step(setpoint: 1.0, measurement: 0.5, dt: 0.1);

        Assert.Equal(1.0, output, 10);
    }

    [Fact]
    public void Integral_AccumulatesOverTime()
    {
        var pid = new PidController(new PidConfig { Kp = 0.0, Ki = 1.0 });

        double first = pid.Step(setpoint: 1.0, measurement: 0.0, dt: 0.1);
        Assert.Equal(0.1, first, 10);

        double second = pid.Step(setpoint: 1.0, measurement: 0.0, dt: 0.1);
        Assert.Equal(0.2, second, 10);
    }

    [Fact]
    public void DerivativeOnError_ReactsToChanges()
    {
        // dt = 1s so the derivative term equals the error change directly.
        var pid = new PidController(new PidConfig
        {
            Kp = 0.0,
            Kd = 0.5,
            DerivativeMode = DerivativeMode.OnError
        });

        pid.Step(setpoint: 1.0, measurement: 0.0, dt: 1.0); // state: derivative 0
        double output = pid.Step(setpoint: 1.0, measurement: 0.4, dt: 1.0);

        // dError = (1.0-0.4) - (1.0-0.0) = -0.4 -> derivative term = -0.2.
        Assert.Equal(-0.2, output, 10);
    }

    [Fact]
    public void Output_IsClampedToLimits()
    {
        var pid = new PidController(new PidConfig
        {
            Kp = 10.0,
            OutputMin = -1.0,
            OutputMax = 1.0
        });

        double output = pid.Step(setpoint: 1.0, measurement: 0.0, dt: 0.1);

        Assert.Equal(1.0, output, 10);
    }

    [Fact]
    public void Integral_IsClampedWithWindupLimit()
    {
        var pid = new PidController(new PidConfig
        {
            Ki = 1.0,
            IntegralLimit = 0.25
        });

        for (int i = 0; i < 5; i++)
            pid.Step(setpoint: 1.0, measurement: 0.0, dt: 0.1);

        Assert.Equal(0.25, pid.Integral, 10);
    }

    [Fact]
    public void DerivativeOnMeasurement_IgnoresSetpointSteps()
    {
        var pid = new PidController(new PidConfig
        {
            Kp = 0.0,
            Kd = 1.0,
            DerivativeMode = DerivativeMode.OnMeasurement
        });

        // Setpoint jumps here would produce derivative kick in OnError mode.
        pid.Step(setpoint: 0.0, measurement: 0.0, dt: 1.0);
        double output = pid.Step(setpoint: 1.0, measurement: 0.0, dt: 1.0);

        Assert.Equal(0.0, output, 10);
    }

    [Fact]
    public void Reset_ClearsIntegral()
    {
        var pid = new PidController(new PidConfig { Ki = 1.0 });

        pid.Step(setpoint: 1.0, measurement: 0.0, dt: 0.5);
        Assert.NotEqual(0.0, pid.Integral, 10);

        pid.Reset();

        Assert.Equal(0.0, pid.Integral);
    }

    [Fact]
    public void Step_WithNonPositiveDt_Throws()
    {
        var pid = new PidController(new PidConfig());

        Assert.Throws<System.ArgumentOutOfRangeException>(() => pid.Step(1.0, 0.0, dt: 0.0));
    }

    [Fact]
    public void RepeatedRuns_AreDeterministic()
    {
        double RunOnce()
        {
            var pid = new PidController(new PidConfig
            {
                Kp = 2.0,
                Ki = 0.5,
                Kd = 1.0,
                IntegralLimit = 1.0
            });

            double last = 0.0;
            for (int i = 0; i < 100; i++)
            {
                double measurement = 0.2 * Math.Sin(i * 0.1);
                last = pid.Step(setpoint: 0.0, measurement, dt: 0.01);
            }
            return last;
        }

        Assert.Equal(RunOnce(), RunOnce(), 12);
    }
}