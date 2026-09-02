using System;
using HelixForge.Simulation;

namespace HelixForge.Console;

/// <summary>
/// Barometer device sample: climbs from 0m to 100m and prints pressure/altitude.
/// </summary>
internal static class BaroSample
{
    public static void Run(TimeSpan timeStep, TimeSpan duration)
    {
        System.Console.WriteLine("=== Barometer Sample ===");
        System.Console.WriteLine("Climbs at 20 m/s and shows pressure/altitude response.");
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

        int step = 0;
        for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += timeStep)
        {
            baro.SetAltitude(currentTime.TotalSeconds * 20.0); // 20 m/s
            baro.Update(timeStep);
            var reading = baro.Read();

            if (step % 10 == 0)
            {
                System.Console.WriteLine(
                    $"[{currentTime.TotalMilliseconds,5:F0}ms] " +
                    $"pressure={reading.Pressure,8:F2} hPa | alt={reading.Altitude,6:F1} m");
            }
            step++;
        }
    }
}
