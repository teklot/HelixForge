using HelixForge;
using HelixForge.Simulation;
using HelixForge.Telemetry;

namespace HelixForge.Tests.Integration;

public class GoldenPathTests
{
    [Fact]
    public void FullSimulation_DeterministicAndObservable()
    {
        // Arrange
        var registry = new DeviceRegistry();
        var imuConfig = new ImuSimConfig
        {
            InitialOrientation = new Vector3(0.2, 0.1, 0.0),
            AccelerometerNoise = 0.005,
            GyroscopeNoise = 0.002,
            RandomSeed = 42
        };
        registry.Register(new SimImuDevice("imu-01", imuConfig));
        registry.Register(new SimMotorDevice("motor-left", new MotorSimConfig()));
        registry.Register(new SimMotorDevice("motor-right", new MotorSimConfig()));

        var telemetry = new TelemetryBus();
        var events = new List<TelemetryEvent>();
        telemetry.AddSink(new DelegateSink(e => events.Add(e)));

        var config = new SimulationConfig
        {
            TimeStep = TimeSpan.FromMilliseconds(10),
            RandomSeed = 42
        };
        var engine = new SimulationEngine(registry, telemetry, config);

        // Act
        engine.Run(TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(100, engine.StepCount);
        Assert.Equal(TimeSpan.FromSeconds(1), engine.CurrentTime);
        Assert.True(events.Count > 0, "Telemetry should have received events");

        // Verify telemetry contains expected device data
        var deviceIds = events.Select(e => e.DeviceId).Distinct().ToList();
        Assert.Contains("imu-01", deviceIds);

        // Verify determinism by running again
        var registry2 = new DeviceRegistry();
        var imuConfig2 = new ImuSimConfig
        {
            InitialOrientation = new Vector3(0.2, 0.1, 0.0),
            AccelerometerNoise = 0.005,
            GyroscopeNoise = 0.002,
            RandomSeed = 42
        };
        registry2.Register(new SimImuDevice("imu-01", imuConfig2));
        registry2.Register(new SimMotorDevice("motor-left", new MotorSimConfig()));
        registry2.Register(new SimMotorDevice("motor-right", new MotorSimConfig()));

        var config2 = new SimulationConfig
        {
            TimeStep = TimeSpan.FromMilliseconds(10),
            RandomSeed = 42
        };
        var engine2 = new SimulationEngine(registry2, null, config2);
        engine2.Run(TimeSpan.FromSeconds(1));

        var finalReading1 = registry.GetByType<IImuDevice>()!.Read();
        var finalReading2 = registry2.GetByType<IImuDevice>()!.Read();

        Assert.Equal(finalReading1.Orientation.X, finalReading2.Orientation.X, 10);
        Assert.Equal(finalReading1.Orientation.Y, finalReading2.Orientation.Y, 10);

        telemetry.Dispose();
    }
}
