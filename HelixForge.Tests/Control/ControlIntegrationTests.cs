using HelixForge;
using HelixForge.Control;
using HelixForge.Simulation;

namespace HelixForge.Tests.Control;

public class ControlIntegrationTests
{
    [Fact]
    public void PidController_StabilizesSimulatedUav()
    {
        var registry = new DeviceRegistry();
        var imuConfig = new ImuSimConfig
        {
            InitialOrientation = new Vector3(0.5, 0.0, 0.0),
            AccelerometerNoise = 0.0,
            GyroscopeNoise = 0.0,
            RandomSeed = 42
        };
        var imu = new SimImuDevice("imu-01", imuConfig);
        var motorL = new SimMotorDevice("motor-left", new MotorSimConfig { ResponseTimeConstant = 0.01 });
        var motorR = new SimMotorDevice("motor-right", new MotorSimConfig { ResponseTimeConstant = 0.01 });

        registry.Register(imu);
        registry.Register(motorL);
        registry.Register(motorR);

        var engine = new SimulationEngine(registry, null,
            new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10), RandomSeed = 42 });

        var pid = new PidController(new PidConfig
        {
            Kp = 2.0,
            Ki = 0.1,
            Kd = 0.5,
            OutputMin = -0.5,
            OutputMax = 0.5,
            IntegralLimit = 0.2,
            DerivativeMode = DerivativeMode.OnMeasurement
        });

        engine.Run(TimeSpan.FromSeconds(5), _ =>
        {
            var reading = imu.Read();
            double correction = pid.Step(setpoint: 0.0, measurement: reading.Orientation.X, dt: 0.01);

            motorL.SetThrottle(Math.Clamp(0.5 + correction, 0.0, 1.0));
            motorR.SetThrottle(Math.Clamp(0.5 - correction, 0.0, 1.0));
        });

        var finalReading = imu.Read();
        Assert.InRange(Math.Abs(finalReading.Orientation.X), 0.0, 0.1);
    }

    [Fact]
    public void MadgwickInLoop_StaysStable()
    {
        var registry = new DeviceRegistry();
        var imu = new SimImuDevice("imu-01", new ImuSimConfig
        {
            AccelerometerNoise = 0.0,
            GyroscopeNoise = 0.0,
            RandomSeed = 7
        });
        registry.Register(imu);

        var engine = new SimulationEngine(registry, null,
            new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10), RandomSeed = 7 });

        var filter = new MadgwickFilter(new MadgwickFilterConfig { Beta = 0.5 });

        engine.Run(TimeSpan.FromSeconds(2), _ =>
        {
            var reading = imu.Read();
            filter.Update(TimeSpan.FromMilliseconds(10), reading.AngularVelocity, reading.Acceleration);
        });

        // No rotational input: the filter should not drift from level orientation.
        var orientation = filter.Orientation;
        Assert.InRange(Math.Abs(orientation.X), 0.0, 0.2);
        Assert.InRange(Math.Abs(orientation.Y), 0.0, 0.2);
    }
}