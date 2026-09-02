using System;
using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Console;

/// <summary>
/// GPS device sample: high dropout rate shows cyclic Fix3D/NoFix transitions.
/// </summary>
internal static class GpsSample
{
    public static void Run(TimeSpan timeStep, TimeSpan duration)
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

        int step = 0;
        for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += timeStep)
        {
            gps.Update(timeStep);
            var reading = gps.Read();

            if (step % 25 == 0)
            {
                System.Console.WriteLine(
                    $"[{currentTime.TotalMilliseconds,4:F0}ms] " +
                    $"fix={reading.FixStatus,-6} lat={reading.Latitude,9:F5} lon={reading.Longitude,9:F5} alt={reading.Altitude,7:F1}");
            }
            step++;
        }
    }
}
