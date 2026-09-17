using System;
using HelixForge;
using HelixForge.Control;
using HelixForge.Simulation;

namespace HelixForge.Console;

/// <summary>
/// Control library sample: attitude estimation (complementary + Madgwick) fused from a
/// simulated IMU, then trapezoidal trajectory tracking with a PID controller and Kalman-smoothed feedback.
/// </summary>
internal static class ControlSample
{
    public static void Run(TimeSpan timeStep, TimeSpan duration)
    {
        System.Console.WriteLine("=== Control Library Sample ===");
        System.Console.WriteLine("Part 1: complementary vs. Madgwick attitude estimation on a simulated IMU.");
        System.Console.WriteLine("Part 2: trapezoidal profile + PID tracking with Kalman-smoothed feedback.");
        System.Console.WriteLine();

        int printEvery = Math.Max(1, (int)(TimeSpan.FromSeconds(0.5).Ticks / timeStep.Ticks));

        AttitudeEstimation(timeStep, (duration.Ticks / 2 + timeStep.Ticks - 1) / timeStep.Ticks * timeStep, printEvery);
        TrajectoryTracking(timeStep, (duration.Ticks - (duration.Ticks / 2) + timeStep.Ticks - 1) / timeStep.Ticks * timeStep, printEvery);
    }

    private static void AttitudeEstimation(TimeSpan timeStep, TimeSpan duration, int printEvery)
    {
        System.Console.WriteLine("--- Part 1: Attitude Estimation ---");
        System.Console.WriteLine("Sensors rock in roll/pitch while yawing at 0.6 rad/s.");
        System.Console.WriteLine("            roll(deg)         pitch(deg)         yaw(deg)");
        System.Console.WriteLine("  true   comp   madg   true   comp   madg   true   comp   madg");
        System.Console.WriteLine();

        var imuConfig = new ImuSimConfig
        {
            AccelerometerNoise = 0.01,
            GyroscopeNoise = 0.005,
            AccelerometerDrift = 0.001,
            GyroscopeDrift = 0.001,
            RandomSeed = 7
        };
        var imu = new SimImuDevice("imu-01", imuConfig);
        imu.Initialize();

        var magConfig = new MagSimConfig
        {
            EarthField = new Vector3(25.0, 0.0, -45.0),
            MagneticNoise = 0.2,
            RandomSeed = 7
        };
        var mag = new SimMagDevice("mag-01", magConfig);
        mag.Initialize();

        var complementary = new ComplementaryFilter(new ComplementaryFilterConfig { Alpha = 0.98 });
        var madgwick = new MadgwickFilter(new MadgwickFilterConfig { Beta = 0.15 });
        var currentTime = TimeSpan.Zero;
        int step = 0;

        for (; currentTime < duration; currentTime += timeStep, step++)
        {
            double t = currentTime.TotalSeconds;
            var angularVelocity = new Vector3(
                0.2 * Math.Cos(t),       // d/dt of roll = 0.2 sin(t)
                0.15 * Math.Cos(2.0 * t), // d/dt of pitch = 0.15 cos(2t) / 2
                0.6);                    // constant yaw rate

            imu.SetAngularVelocity(angularVelocity);
            imu.Update(timeStep);
            imu.SetAcceleration(new Vector3(0, 0, -9.81));

            // Magnetometer measures the world field in the sensor frame.
            mag.SetOrientation(imu.Read().Orientation);
            mag.Update(timeStep);

            var reading = imu.Read();
            var field = mag.Read().MagneticField;
            complementary.Update(timeStep, reading.AngularVelocity, reading.Acceleration, field);
            madgwick.Update(timeStep, reading.AngularVelocity, reading.Acceleration, field);

            if (step % printEvery == 0)
            {
                Vector3 truth = reading.Orientation;
                Vector3 comp = complementary.Orientation;
                Vector3 madg = madgwick.Orientation;
                System.Console.WriteLine(
                    $"{RadToDeg(truth.X),6:F1} {RadToDeg(comp.X),6:F1} {RadToDeg(madg.X),6:F1}  " +
                    $"{RadToDeg(truth.Y),6:F1} {RadToDeg(comp.Y),6:F1} {RadToDeg(madg.Y),6:F1}  " +
                    $"{RadToDeg(truth.Z),6:F1} {RadToDeg(comp.Z),6:F1} {RadToDeg(madg.Z),6:F1}");
            }
        }

        System.Console.WriteLine();
        imu.Dispose();
        mag.Dispose();
    }

    private static void TrajectoryTracking(TimeSpan timeStep, TimeSpan duration, int printEvery)
    {
        System.Console.WriteLine("--- Part 2: Trajectory Tracking (PID + Kalman) ---");
        System.Console.WriteLine("A trapezoidal profile is tracked by a PID plant; Kalman smooths the noisy feedback.");
        System.Console.WriteLine("  time   setpoint    raw noise   kalman      actual   pid out");
        System.Console.WriteLine();

        var profile = new TrapezoidalProfile(new TrapezoidalProfileConfig
        {
            MaxVelocity = 1.5,
            MaxAcceleration = 2.0,
            MaxDeceleration = 2.0
        });
        profile.SetTarget(0.0, 3.0);

        var pid = new PidController(new PidConfig
        {
            Kp = 8.0,
            Ki = 0.5,
            Kd = 2.0,
            OutputMin = -3.0,
            OutputMax = 3.0,
            IntegralLimit = 1.0,
            DerivativeTau = 0.05,
            DerivativeMode = DerivativeMode.OnMeasurement
        });

        var kalman = new ScalarKalmanFilter(new ScalarKalmanConfig
        {
            ProcessNoise = 0.01,
            MeasurementNoise = 0.01,
            InitialCovariance = 10.0
        });

        var noise = new Random(1234);
        double position = 0.0;
        double velocity = 0.0;
        double dt = timeStep.TotalSeconds;
        int step = 0;

        for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += timeStep, step++)
        {
            var setpoint = profile.Evaluate(currentTime);
            double control = pid.Step(setpoint.Position, position, dt);

            velocity += control * dt;
            position += velocity * dt;

            double raw = position + 0.1 * Gaussian(noise);
            double estimate = kalman.Update(raw, dt);

            if (step % printEvery == 0)
            {
                System.Console.WriteLine(
                    $"{currentTime.TotalSeconds,6:F2} " +
                    $"{setpoint.Position,8:F2} " +
                    $"{raw,10:F2} " +
                    $"{estimate,10:F2} " +
                    $"{position,8:F2} " +
                    $"{control,7:F2}");
            }
        }

        System.Console.WriteLine();
    }

    private static double RadToDeg(double radians) => radians * 180.0 / Math.PI;

    private static double Gaussian(Random random)
    {
        double u1 = 1.0 - random.NextDouble();
        double u2 = 1.0 - random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}