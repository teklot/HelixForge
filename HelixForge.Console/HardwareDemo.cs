using System;
using System.Threading;
using HelixForge.Hardware.Drivers;
using HelixForge.Telemetry;

namespace HelixForge.Console;

/// <summary>
/// Golden-path UAV stabilization demo running against physical hardware.
/// </summary>
internal static class HardwareDemo
{
    /// <summary>
    /// Runs the hardware-mode stabilization demo.
    /// </summary>
    /// <param name="telemetry">Telemetry bus for observation output.</param>
    /// <param name="timeStep">Simulation time step.</param>
    /// <param name="duration">Total demo duration.</param>
    public static void Run(TelemetryBus telemetry, TimeSpan timeStep, TimeSpan duration)
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

        System.Console.WriteLine("Hardware initialized. Reading sensors...");
        System.Console.WriteLine();

        int stepCount = 0;

        for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += timeStep)
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
