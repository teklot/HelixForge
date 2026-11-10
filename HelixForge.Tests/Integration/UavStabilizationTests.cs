using HelixForge;
using HelixForge.Control;
using HelixForge.Simulation;

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
        imu.Initialize();
        motorL.Initialize();
        motorR.Initialize();

        var config = new SimulationConfig { TimeStep = TimeSpan.FromMilliseconds(10) };
        var engine = new SimulationEngine(registry, null, config);

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

        // Express the UAV plant in control code (no global physics engine): first-order
        // angular-rate response to differential throttle feeds back into the IMU.
        double angularRate = 0.0;
        const double torqueGain = 5.0;
        const double damping = 0.5;

        engine.Run(TimeSpan.FromSeconds(5), _ =>
        {
            var reading = imu.Read();
            double correction = pid.Step(setpoint: 0.0, measurement: reading.Orientation.X, dt: 0.01);

            motorL.SetThrottle(Math.Clamp(0.5 + correction, 0.0, 1.0));
            motorR.SetThrottle(Math.Clamp(0.5 - correction, 0.0, 1.0));

            angularRate += (torqueGain * correction - damping * angularRate) * 0.01;
            imu.SetAngularVelocity(new Vector3(angularRate, 0.0, 0.0));
        });

        // After 5 seconds, orientation should be close to target
        var finalReading = imu.Read();
        Assert.InRange(Math.Abs(finalReading.Orientation.X), 0.0, 0.1);
    }
}