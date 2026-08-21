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
}
