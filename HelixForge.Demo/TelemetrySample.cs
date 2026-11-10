using System;
using System.IO;
using HelixForge.Simulation;
using HelixForge.Telemetry;

namespace HelixForge.Demo;

/// <summary>
/// Telemetry persistence sample: routes simulation events through a bounded buffered
/// sink into a tidy-long CSV file and reports the archival counters.
/// </summary>
internal static class TelemetrySample
{
    public static void Run(TimeSpan timeStep, TimeSpan duration)
    {
        System.Console.WriteLine("=== Telemetry Persistence Sample ===");
        System.Console.WriteLine("SimulationEngine -> TelemetryBus -> BufferedSink -> CsvSink");
        System.Console.WriteLine("Rows are tidy-long: timestamp_ms,device_id,metric,value.");
        System.Console.WriteLine();

        string path = Path.Combine(
            Path.GetTempPath(),
            "helixforge-telemetry-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv");

        var registry = new DeviceRegistry();
        var imu = new SimImuDevice("imu-01", new ImuSimConfig { RandomSeed = 42 });
        var gps = new SimGpsDevice("gps-01", new GpsSimConfig { RandomSeed = 42 });
        var motor = new SimMotorDevice("motor-01", new MotorSimConfig
        {
            MaxRpm = 10000,
            ResponseTimeConstant = 0.05,
            MaxCurrentAmps = 15.0
        });
        registry.Register(imu);
        registry.Register(gps);
        registry.Register(motor);

        imu.Initialize();
        gps.Initialize();
        motor.Initialize();
        motor.SetThrottle(0.6);

        var bus = new TelemetryBus();
        var csv = new CsvSink(path);
        var buffered = new BufferedSink(csv, capacity: 8192, TelemetryDropPolicy.DropOldest);
        bus.AddSink(buffered);

        var engine = new SimulationEngine(registry, bus, new SimulationConfig
        {
            TimeStep = timeStep
        });
        engine.Run(duration);

        buffered.Flush(TimeSpan.FromSeconds(10));
        csv.Flush();
        bus.Dispose();

        System.Console.WriteLine($"CSV file : {path}");
        System.Console.WriteLine($"Rows     : {csv.RowsWritten}");
        System.Console.WriteLine($"Dropped  : {buffered.DroppedCount}");
        System.Console.WriteLine($"Failed   : {buffered.FailedCount}");
        System.Console.WriteLine($"Pending  : {buffered.PendingCount}");
        System.Console.WriteLine();
        System.Console.WriteLine("First rows:");

        int shown = 0;
        foreach (string line in File.ReadLines(path))
        {
            System.Console.WriteLine("  " + line);
            if (++shown == 8)
                break;
        }

        System.Console.WriteLine();
    }
}
