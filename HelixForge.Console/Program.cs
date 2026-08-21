using System;
using System.Threading;
using HelixForge;
using HelixForge.Hardware.Drivers;
using HelixForge.Simulation;
using HelixForge.Telemetry;

namespace HelixForge.Console;

/// <summary>
/// Golden Path Demo: UAV stabilization system.
/// Default mode runs in simulation; pass --mode real to use physical hardware.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        bool useRealHardware = args is ["--mode", "real"];

        System.Console.WriteLine("=== HelixForge UAV Stabilization Demo ===");
        System.Console.WriteLine($"Mode: {(useRealHardware ? "REAL HARDWARE" : "Simulation")}");
        System.Console.WriteLine();

        var timeStep = TimeSpan.FromMilliseconds(10); // 100Hz
        var telemetry = new TelemetryBus();
        telemetry.AddSink(new ConsoleSink());

        if (useRealHardware)
        {
            RunHardwareDemo(telemetry, timeStep);
        }
        else
        {
            RunSimulationDemo(telemetry, timeStep);
        }
    }

    static void RunSimulationDemo(TelemetryBus telemetry, TimeSpan timeStep)
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
            ResponseTimeConstant = 0.05
        };

        var registry = new DeviceRegistry();
        var imu = new SimImuDevice("imu-01", imuConfig);
        var motorLeft = new SimMotorDevice("motor-left", motorConfig);
        var motorRight = new SimMotorDevice("motor-right", motorConfig);

        registry.Register(imu);
        registry.Register(motorLeft);
        registry.Register(motorRight);

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

        engine.Run(TimeSpan.FromSeconds(5), currentTime =>
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
                System.Console.WriteLine(
                    $"[{currentTime.TotalSeconds,6:F2}s] " +
                    $"Roll={reading.Orientation.X,8:F4} rad | " +
                    $"Error={error,8:F4} | " +
                    $"L={leftThrottle,5:F3} R={rightThrottle,5:F3} | " +
                    $"RPM L={motorLeft.CurrentRpm,7:F0} R={motorRight.CurrentRpm,7:F0}");
            }
        });

        System.Console.WriteLine();
        System.Console.WriteLine("=== Simulation Complete ===");
        System.Console.WriteLine($"Final orientation: {imu.Read().Orientation}");
        System.Console.WriteLine($"Steps executed: {engine.StepCount}");
    }

    static void RunHardwareDemo(TelemetryBus telemetry, TimeSpan timeStep)
    {
        System.Console.WriteLine("Initializing hardware devices...");
        System.Console.WriteLine();

        using var imu = new Bmi160ImuDevice("imu-01", 1, 0x68);
        using var motorLeft = new PwmMotorDevice("motor-left", 0, 0);
        using var motorRight = new PwmMotorDevice("motor-right", 0, 1);

        try
        {
            imu.Initialize();
            motorLeft.Initialize();
            motorRight.Initialize();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Hardware initialization failed: {ex.Message}");
            System.Console.WriteLine("Ensure hardware is connected and try again.");
            return;
        }

        System.Console.WriteLine("Hardware initialized. Reading sensors for 5 seconds...");
        System.Console.WriteLine();

        int stepCount = 0;

        for (var currentTime = TimeSpan.Zero; currentTime < TimeSpan.FromSeconds(5); currentTime += timeStep)
        {
            imu.Update(timeStep);
            var reading = imu.Read();

            // Simple rate-damping control on roll axis
            double dampingGain = 0.05;
            double baseThrottle = 0.3;
            double correction = dampingGain * reading.AngularVelocity.X;
            double leftThrottle = Math.Clamp(baseThrottle + correction, 0.0, 1.0);
            double rightThrottle = Math.Clamp(baseThrottle - correction, 0.0, 1.0);

            motorLeft.SetThrottle(leftThrottle);
            motorRight.SetThrottle(rightThrottle);

            if (currentTime.TotalMilliseconds % 500 < timeStep.TotalMilliseconds)
            {
                System.Console.WriteLine(
                    $"[{currentTime.TotalSeconds,6:F2}s] " +
                    $"Accel=({reading.Acceleration.X,6:F2},{reading.Acceleration.Y,6:F2},{reading.Acceleration.Z,6:F2}) m/s² | " +
                    $"Gyro=({reading.AngularVelocity.X,6:F2},{reading.AngularVelocity.Y,6:F2},{reading.AngularVelocity.Z,6:F2}) rad/s | " +
                    $"L={leftThrottle,4:F2} R={rightThrottle,4:F2}");
            }

            stepCount++;
            Thread.Sleep(timeStep);
        }

        System.Console.WriteLine();
        System.Console.WriteLine("=== Hardware Demo Complete ===");
        System.Console.WriteLine($"Steps executed: {stepCount}");
    }
}
