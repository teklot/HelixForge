using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class SimulationEngineTests
{
    [Fact]
    public void Step_AdvancesTime()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));
        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10) };
        var engine = new SimulationEngine(registry, null, config);

        engine.Step();

        Assert.Equal(TimeSpan.FromMilliseconds(10), engine.CurrentTime);
        Assert.Equal(1, engine.StepCount);
    }

    [Fact]
    public void Run_ExecutesCorrectNumberOfSteps()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));
        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10) };
        var engine = new SimulationEngine(registry, null, config);

        engine.Run(TimeSpan.FromSeconds(1)); // 100 steps at 10ms

        Assert.Equal(100, engine.StepCount);
        Assert.Equal(TimeSpan.FromSeconds(1), engine.CurrentTime);
    }

    [Fact]
    public void Run_WithCallback_InvokesCallback()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));
        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10) };
        var engine = new SimulationEngine(registry, null, config);

        int callbackCount = 0;
        engine.Run(TimeSpan.FromMilliseconds(50), _ => callbackCount++);

        Assert.Equal(5, callbackCount);
    }

    [Fact]
    public void Run_MaxIterations_StopsEarly()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));
        var config = new SimulationConfig
        {
            TimeStep = TimeSpan.FromMilliseconds(10),
            MaxIterations = 5
        };
        var engine = new SimulationEngine(registry, null, config);

        engine.Run(TimeSpan.FromSeconds(10));

        Assert.Equal(5, engine.StepCount);
    }

    [Fact]
    public void Run_MaxIterations_AppliesPerRunCall()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));
        var config = new SimulationConfig
        {
            TimeStep = TimeSpan.FromMilliseconds(10),
            MaxIterations = 5
        };
        var engine = new SimulationEngine(registry, null, config);

        engine.Run(TimeSpan.FromSeconds(10));
        engine.Run(TimeSpan.FromSeconds(10));

        Assert.Equal(10, engine.StepCount);
    }

    [Fact]
    public void Reset_ReturnsToInitialState()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));
        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10) };
        var engine = new SimulationEngine(registry, null, config);

        engine.Run(TimeSpan.FromSeconds(1));
        engine.Reset();

        Assert.Equal(TimeSpan.Zero, engine.CurrentTime);
        Assert.Equal(0, engine.StepCount);
    }

    [Fact]
    public void Step_FiresEvent()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));
        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10) };
        var engine = new SimulationEngine(registry, null, config);

        SimulationStepCompletedEventArgs? eventArgs = null;
        engine.StepCompleted += (_, e) => eventArgs = e;

        engine.Step();

        Assert.NotNull(eventArgs);
        Assert.Equal(1, eventArgs!.StepNumber);
        Assert.Equal(TimeSpan.FromMilliseconds(10), eventArgs.CurrentTime);
    }

    [Fact]
    public void Step_PublishesTelemetry()
    {
        var registry = new DeviceRegistry();
        var imu = new SimImuDevice("imu-01", new ImuSimConfig());
        registry.Register(imu);

        var telemetry = new HelixForge.Telemetry.TelemetryBus();
        var events = new List<HelixForge.Telemetry.TelemetryEvent>();
        telemetry.AddSink(new HelixForge.Telemetry.DelegateSink(e => events.Add(e)));

        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10) };
        var engine = new SimulationEngine(registry, telemetry, config);

        engine.Step();

        Assert.True(events.Count > 0);
        Assert.Contains(events, e => e.DeviceId == "imu-01" && e.MetricName == "throttle" || e.MetricName.StartsWith("orientation"));

        telemetry.Dispose();
    }

    [Fact]
    public void Step_UpdatesServoDevices()
    {
        var registry = new DeviceRegistry();
        var servo = new SimServoDevice("servo-01", new ServoSimConfig { SlewRate = 100.0 });
        servo.Initialize();
        registry.Register(servo);

        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10) };
        var engine = new SimulationEngine(registry, null, config);

        servo.SetAngle(90.0);
        engine.Step();

        // 10ms at 100 deg/s = 1 degree
        Assert.Equal(1.0, servo.Angle, 4);
    }

    [Fact]
    public void Step_UpdatesMotorDevices()
    {
        var registry = new DeviceRegistry();
        var motor = new SimMotorDevice("motor-01", new MotorSimConfig { ResponseTimeConstant = 0.01, Deadband = 0.0 });
        motor.Initialize();
        registry.Register(motor);

        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10) };
        var engine = new SimulationEngine(registry, null, config);

        motor.SetThrottle(1.0);
        engine.Step();

        // After one step with a fast time constant, throttle should move toward target (>0).
        Assert.True(motor.Throttle > 0.0, "Engine must drive motor updates toward the commanded throttle.");
    }

    [Fact]
    public void Step_PublishesTelemetryForNewDevices()
    {
        var registry = new DeviceRegistry();
        registry.Register(new SimMagDevice("mag-01", new MagSimConfig()));
        registry.Register(new SimBarometerDevice("baro-01", new BarometerSimConfig()));
        registry.Register(new SimServoDevice("servo-01", new ServoSimConfig()));
        registry.Register(new SimGpsDevice("gps-01", new GpsSimConfig()));

        var telemetry = new HelixForge.Telemetry.TelemetryBus();
        var events = new List<HelixForge.Telemetry.TelemetryEvent>();
        telemetry.AddSink(new HelixForge.Telemetry.DelegateSink(e => events.Add(e)));

        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10) };
        var engine = new SimulationEngine(registry, telemetry, config);

        engine.Step();

        Assert.Contains(events, e => e.DeviceId == "mag-01" && e.MetricName == "magnetic_field");
        Assert.Contains(events, e => e.DeviceId == "baro-01" && e.MetricName == "pressure");
        Assert.Contains(events, e => e.DeviceId == "baro-01" && e.MetricName == "altitude");
        Assert.Contains(events, e => e.DeviceId == "servo-01" && e.MetricName == "angle");
        Assert.Contains(events, e => e.DeviceId == "gps-01" && e.MetricName == "fix_status");

        telemetry.Dispose();
    }
}
