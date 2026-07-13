using HelixForge;
using HelixForge.Simulation;
using HelixForge.Telemetry;

namespace HelixForge.Tests.Integration;

public class UavStabilizationTests
{
    [Fact]
    public void PidController_ConvergesToTarget()
    {
        var registry = new DeviceRegistry();
        var imuConfig = new ImuSimConfig
        {
            InitialOrientation = new Vector3(0.5, 0.0, 0.0), // Start tilted
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

        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10), RandomSeed = 42 };
        var engine = new SimulationEngine(registry, null, config);

        double kp = 2.0, ki = 0.1, kd = 0.5;
        double integral = 0.0, prevError = 0.0;
        double target = 0.0;

        engine.Run(TimeSpan.FromSeconds(5), _ =>
        {
            var reading = imu.Read();
            double error = target - reading.Orientation.X;
            integral += error * config.TimeStep.TotalSeconds;
            double derivative = (error - prevError) / config.TimeStep.TotalSeconds;
            double correction = kp * error + ki * integral + kd * derivative;
            prevError = error;

            motorL.SetThrottle(Math.Clamp(0.5 + correction, 0.0, 1.0));
            motorR.SetThrottle(Math.Clamp(0.5 - correction, 0.0, 1.0));
        });

        // After 5 seconds, orientation should be close to target
        var finalReading = imu.Read();
        Assert.InRange(Math.Abs(finalReading.Orientation.X), 0.0, 0.1);
    }
}
