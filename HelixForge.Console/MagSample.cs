using System;
using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Console;

/// <summary>
/// Magnetometer device sample: sweeps sensor yaw and prints the field projection.
/// </summary>
internal static class MagSample
{
    public static void Run(TimeSpan timeStep, TimeSpan duration)
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

        int step = 0;
        for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += timeStep)
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
            step++;
        }
    }
}
