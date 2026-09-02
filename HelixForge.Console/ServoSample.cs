using System;
using HelixForge.Simulation;

namespace HelixForge.Console;

/// <summary>
/// Servo device sample: commands 0 -> 180 -> 0 and shows slew limiting + clamping.
/// </summary>
internal static class ServoSample
{
    public static void Run(TimeSpan timeStep, TimeSpan duration)
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

        int step = 0;
        for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += timeStep)
        {
            servo.Update(timeStep);

            if (step % 10 == 0)
            {
                System.Console.WriteLine(
                    $"[{currentTime.TotalMilliseconds,4:F0}ms] " +
                    $"target={servo.TargetAngle,5:F0}° | angle={servo.Angle,6:F1}°");
            }

            if (servo.TargetAngle != targets[targetIndex] && Math.Abs(servo.Angle - servo.TargetAngle) < 0.5)
            {
                targetIndex = (targetIndex + 1) % targets.Length;
                servo.SetAngle(targets[targetIndex]);
            }
            step++;
        }
    }
}
