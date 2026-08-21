using BenchmarkDotNet.Attributes;
using HelixForge;
using HelixForge.Simulation;
using HelixForge.Telemetry;

namespace HelixForge.Benchmark;

[MemoryDiagnoser]
public class SimulationEngineBenchmark
{
    private SimulationEngine _engine = null!;
    private SimulationEngine _engineWithTelemetry = null!;
    private DeviceRegistry _registry = null!;

    [GlobalSetup]
    public void Setup()
    {
        _registry = new DeviceRegistry();
        _registry.Register(new SimImuDevice("imu-01", new ImuSimConfig()));
        _registry.Register(new SimMotorDevice("motor-01", new MotorSimConfig()));

        var config = new SimulationConfig
        {
            TimeStep = TimeSpan.FromMilliseconds(0.01), // 100kHz for benchmark
            RandomSeed = 42
        };

        _engine = new SimulationEngine(_registry, null, config);

        var telemetry = new TelemetryBus();
        telemetry.AddSink(new DelegateSink(_ => { }));
        _engineWithTelemetry = new SimulationEngine(_registry, telemetry, config);
    }

    [Benchmark]
    public void Step()
    {
        _engine.Step();
    }

    [Benchmark]
    public void Run1000Steps()
    {
        for (int i = 0; i < 1000; i++)
        {
            _engine.Step();
        }
    }

    [Benchmark]
    public void StepWithTelemetry()
    {
        _engineWithTelemetry.Step();
    }
}
