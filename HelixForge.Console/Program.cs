using System;
using System.Threading;
using HelixForge;
using HelixForge.Hardware.Drivers;
using HelixForge.Simulation;
using HelixForge.Telemetry;

namespace HelixForge.Console;

/// <summary>
/// HelixForge demos.
/// Default runs the Golden Path UAV stabilization simulation; pass --mode real for physical hardware.
/// Run a specific device sample with --sample &lt;mag|baro|servo|gps&gt;.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        var timeStep = TimeSpan.FromMilliseconds(10); // 100Hz
        var telemetry = new TelemetryBus();
        telemetry.AddSink(new ConsoleSink());

        string? sample = FindArg(args, "--sample");
        if (sample != null)
        {
            RunSample(sample, timeStep);
            return;
        }

        bool useRealHardware = args is ["--mode", "real"];

        System.Console.WriteLine("=== HelixForge UAV Stabilization Demo ===");
        System.Console.WriteLine($"Mode: {(useRealHardware ? "REAL HARDWARE" : "Simulation")}");
        System.Console.WriteLine();
        System.Console.WriteLine("Run `--sample <mag|baro|servo|gps>` for per-device demos.");
        System.Console.WriteLine();

        if (useRealHardware)
        {
            RunHardwareDemo(telemetry, timeStep);
        }
        else
        {
            RunSimulationDemo(telemetry, timeStep);
        }
    }

    static string? FindArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }

    static void RunSample(string name, TimeSpan timeStep)
    {
        switch (name.ToLowerInvariant())
        {
            case "mag":
                RunMagSample(timeStep);
                break;
            case "baro":
                RunBarometerSample(timeStep);
                break;
            case "servo":
                RunServoSample(timeStep);
                break;
            case "gps":
                RunGpsSample(timeStep);
                break;
            default:
                System.Console.WriteLine($"Unknown sample '{name}'. Choose from: mag, baro, servo, gps.");
                break;
        }
    }

    static void RunMagSample(TimeSpan timeStep)
    {
        System.Console.WriteLine("=== Magnetometer Sample ===");
        System.Console.WriteLine("Rotates the sensor through 180 degrees of yaw and shows the field projection.");
        System.Console.WriteLine();

        var config = new MagSimConfig
        {
            EarthField = new Vector3(25.0, 0.0, -45.0),
            MagneticNoise = 0.2,
            RandomSeed = 42
        };
        var mag = new SimMagDevice("mag-01", config);
        mag.Initialize();

        for (int step = 0; step < 60; step++)
        {
            double yaw = step * 3.0 * Math.PI / 180.0;
            mag.SetOrientation(new Vector3(0.0, 0.0, yaw));
            mag.Update(timeStep);
            var reading = mag.Read();

            if (step % 10 == 0)
            {
                double heading = Math.Atan2(reading.MagneticField.Y, reading.MagneticField.X) * 180.0 / Math.PI;
                System.Console.WriteLine(
                    $"[yaw {step * 3,4:F0} deg] " +
                    $"B=({reading.MagneticField.X,7:F1},{reading.MagneticField.Y,7:F1},{reading.MagneticField.Z,7:F1}) µT | " +
                    $"xy heading={heading,6:F1} deg");
            }
        }
    }

    static void RunBarometerSample(TimeSpan timeStep)
    {
        System.Console.WriteLine("=== Barometer Sample ===");
        System.Console.WriteLine("Climbs from 0m to 100m and shows pressure/altitude response.");
        System.Console.WriteLine();

        var config = new BarometerSimConfig
        {
            SeaLevelPressure = 1013.25,
            AltitudeNoise = 0.5,
            DriftRate = 0.001,
            RandomSeed = 42
        };
        var baro = new SimBarometerDevice("baro-01", config);
        baro.Initialize();

        for (int step = 0; step <= 50; step++)
        {
            baro.SetAltitude(step * 2.0); // 0 -> 100m over 5s at 10ms per step
            baro.Update(timeStep);
            var reading = baro.Read();

            if (step % 10 == 0)
            {
                System.Console.WriteLine(
                    $"[{step * 10,3:D3}ms] " +
                    $"pressure={reading.Pressure,8:F2} hPa | alt={reading.Altitude,6:F1} m");
            }
        }
    }

    static void RunServoSample(TimeSpan timeStep)
    {
        System.Console.WriteLine("=== Servo Sample ===");
        System.Console.WriteLine("Commands 0 -> 180 -> 0 with a 60 deg/s slew rate, proving slew limiting and clamping.");
        System.Console.WriteLine();

        var config = new ServoSimConfig
        {
            MinAngle = 0.0,
            MaxAngle = 180.0,
            SlewRate = 60.0,
            InitialAngle = 0.0
        };
        var servo = new SimServoDevice("servo-01", config);
        servo.Initialize();

        double[] targets = { 180.0, 0.0 };
        int targetIndex = 0;
        servo.SetAngle(targets[0]);

        for (int step = 0; step < 130; step++)
        {
            servo.Update(timeStep);

            if (step % 10 == 0)
            {
                System.Console.WriteLine(
                    $"[{step * 10,4:D3}ms] " +
                    $"target={servo.TargetAngle,5:F0}° | angle={servo.Angle,6:F1}°");
            }

            if (servo.TargetAngle != targets[targetIndex] && Math.Abs(servo.Angle - servo.TargetAngle) < 0.5)
            {
                targetIndex = (targetIndex + 1) % targets.Length;
                servo.SetAngle(targets[targetIndex]);
            }
        }
    }

    static void RunGpsSample(TimeSpan timeStep)
    {
        System.Console.WriteLine("=== GPS Signal-Loss Sample ===");
        System.Console.WriteLine("High dropout rate drives cyclic Fix3D <-> NoFix transitions with large uncertainty.");
        System.Console.WriteLine();

        var config = new GpsSimConfig
        {
            PositionNoise = 1.0,
            UncertaintyDuringDropout = 5000.0,
            DropoutRate = 4.0,
            MaxDropoutDuration = 0.8,
            BaseFixStatus = GpsFixStatus.Fix3D,
            RandomSeed = 42
        };
        var gps = new SimGpsDevice("gps-01", config);
        gps.Initialize();

        for (int step = 0; step < 300; step++)
        {
            gps.Update(timeStep);
            var reading = gps.Read();

            if (step % 25 == 0)
            {
                System.Console.WriteLine(
                    $"[{step * 10,4:D3}ms] " +
                    $"fix={reading.FixStatus,-6} lat={reading.Latitude,9:F5} lon={reading.Longitude,9:F5} alt={reading.Altitude,7:F1}");
            }
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
