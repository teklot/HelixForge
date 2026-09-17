using System;
using System.Collections.Generic;
using HelixForge;
using HelixForge.Control;
using HelixForge.Simulation;
using HelixForge.Telemetry;

namespace HelixForge.Console;

/// <summary>
/// Golden-path UAV stabilization demo running entirely in simulation.
/// </summary>
internal static class SimulationDemo
{
    /// <summary>
    /// Runs the simulation-mode stabilization demo.
    /// </summary>
    /// <param name="telemetry">Telemetry bus for observation output.</param>
    /// <param name="timeStep">Simulation time step.</param>
    /// <param name="duration">Total simulation duration.</param>
    public static void Run(TelemetryBus telemetry, TimeSpan timeStep, TimeSpan duration)
    {
        var simConfig = new SimulationConfig
        {
            TimeStep = timeStep,
            RandomSeed = 42
        };

        var imuConfig = new ImuSimConfig
        {
            AccelerometerNoise = 0.005,
            GyroscopeNoise = 0.002,
            InitialOrientation = new Vector3(0.2, 0.1, 0.0) // Starting tilted
        };

        var motorConfig = new MotorSimConfig
        {
            MaxRpm = 10000,
            ResponseTimeConstant = 0.05,
            MaxCurrentAmps = 15.0
        };

        var registry = new DeviceRegistry();
        var imu = new SimImuDevice("imu-01", imuConfig);
        var motorLeft = new SimMotorDevice("motor-left", motorConfig);
        var motorRight = new SimMotorDevice("motor-right", motorConfig);

        var batteryConfig = new BatterySimConfig
        {
            FullVoltage = 12.6,
            EmptyVoltage = 9.0,
            InternalResistance = 0.05,
            CapacityAh = 5.0,
            InitialCharge = 1.0
        };
        var battery = new SimBatteryDevice("battery-01", batteryConfig,
            new List<ICurrentConsumer> { motorLeft, motorRight });

        registry.Register(imu);
        registry.Register(motorLeft);
        registry.Register(motorRight);
        registry.Register(battery);

        imu.Initialize();
        motorLeft.Initialize();
        motorRight.Initialize();
        battery.Initialize();

        var engine = new SimulationEngine(registry, telemetry, simConfig);

        // PID controller constants
        double targetAngle = 0.0;
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

        System.Console.WriteLine("Starting simulation...");
        System.Console.WriteLine($"Time step: {timeStep.TotalMilliseconds}ms");
        System.Console.WriteLine($"Target angle: {targetAngle} rad");
        System.Console.WriteLine();

        // The simulation is device-scoped (no global physics engine), so the plant that
        // connects motor torque back to vehicle attitude is expressed here as a simple
        // first-order angular-rate response on the IMU.
        double angularRate = 0.0;
        double torqueGain = 5.0;    // rad/s^2 per unit of differential throttle
        double damping = 0.5;       // rad/s per rad/s of angular rate

        engine.Run(duration, currentTime =>
        {
            var reading = imu.Read();

            // PID control on roll axis (X orientation)
            double correction = pid.Step(targetAngle, reading.Orientation.X, timeStep.TotalSeconds);
            double error = targetAngle - reading.Orientation.X;

            // Distribute correction to motors
            double baseThrottle = 0.5;
            double leftThrottle = Math.Clamp(baseThrottle + correction, 0.0, 1.0);
            double rightThrottle = Math.Clamp(baseThrottle - correction, 0.0, 1.0);

            motorLeft.SetThrottle(leftThrottle);
            motorRight.SetThrottle(rightThrottle);

            // Plant: apply angular acceleration from the differential torque, with damping,
            // and feed the resulting rate back to the IMU so the loop is genuinely closed.
            angularRate += (torqueGain * correction - damping * angularRate) * timeStep.TotalSeconds;
            imu.SetAngularVelocity(new Vector3(angularRate, 0.0, 0.0));

            // Print status every 500ms
            if (currentTime.TotalMilliseconds % 500 < timeStep.TotalMilliseconds)
            {
                var b = battery.Read();
                System.Console.WriteLine(
                    $"[{currentTime.TotalSeconds,6:F2}s] " +
                    $"Roll={reading.Orientation.X,8:F4} rad | " +
                    $"Error={error,8:F4} | " +
                    $"L={leftThrottle,5:F3} R={rightThrottle,5:F3} | " +
                    $"RPM L={motorLeft.CurrentRpm,7:F0} R={motorRight.CurrentRpm,7:F0} | " +
                    $"V={b.Voltage,5:F2} SoC={b.ChargeFraction,4:P1}");
            }
        });

        System.Console.WriteLine();
        System.Console.WriteLine("=== Simulation Complete ===");
        System.Console.WriteLine($"Final orientation: {imu.Read().Orientation}");
        System.Console.WriteLine($"Steps executed: {engine.StepCount}");
    }
}
