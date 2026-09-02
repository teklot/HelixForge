using System;
using System.Collections.Generic;
using HelixForge;
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

        var engine = new SimulationEngine(registry, telemetry, simConfig);

        // PID controller constants
        double kp = 2.0;
        double ki = 0.1;
        double kd = 0.5;
        double integral = 0.0;
        double previousError = 0.0;
        double targetAngle = 0.0;

        System.Console.WriteLine("Starting simulation...");
        System.Console.WriteLine($"Time step: {timeStep.TotalMilliseconds}ms");
        System.Console.WriteLine($"Target angle: {targetAngle} rad");
        System.Console.WriteLine();

        engine.Run(duration, currentTime =>
        {
            var reading = imu.Read();

            // PID control on roll axis (X orientation)
            double error = targetAngle - reading.Orientation.X;
            integral += error * timeStep.TotalSeconds;
            double derivative = (error - previousError) / timeStep.TotalSeconds;
            double correction = kp * error + ki * integral + kd * derivative;
            previousError = error;

            // Distribute correction to motors
            double baseThrottle = 0.5;
            double leftThrottle = Math.Clamp(baseThrottle + correction, 0.0, 1.0);
            double rightThrottle = Math.Clamp(baseThrottle - correction, 0.0, 1.0);

            motorLeft.SetThrottle(leftThrottle);
            motorRight.SetThrottle(rightThrottle);

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
