using System;
using HelixForge.Telemetry;

namespace HelixForge.Console;

/// <summary>
/// HelixForge console demos entry point.
/// Default runs the Golden Path UAV stabilization simulation; use --mode real for hardware.
/// Run a specific device sample with --sample &lt;mag|baro|servo|gps&gt;.
/// With no arguments, an interactive options menu is shown.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        var options = CliOptions.Parse(args);

        if (options.ShowHelp)
        {
            CliOptions.PrintHelp();
            return;
        }

        // If any meaningful argument was provided, run non-interactively.
        if (args.Length > 0)
        {
            RunCli(options);
            return;
        }

        RunMenu();
    }

    static void RunCli(CliOptions options)
    {
        var duration = TimeSpan.FromSeconds(options.DurationSeconds);

        if (options.SampleName != null)
        {
            RunSample(options.SampleName, options.TimeStep, duration);
            return;
        }

        var telemetry = new TelemetryBus();
        telemetry.AddSink(new ConsoleSink());

        System.Console.WriteLine("=== HelixForge UAV Stabilization Demo ===");
        System.Console.WriteLine($"Mode: {(options.UseRealHardware ? "REAL HARDWARE" : "Simulation")}");
        System.Console.WriteLine();
        System.Console.WriteLine("Run `--sample <mag|baro|servo|gps>` for per-device demos, or `--help` for options.");
        System.Console.WriteLine();

        if (options.UseRealHardware)
        {
            HardwareDemo.Run(telemetry, options.TimeStep, duration);
        }
        else
        {
            SimulationDemo.Run(telemetry, options.TimeStep, duration);
        }

        telemetry.Dispose();
    }

    static void RunMenu()
    {
        var timeStep = TimeSpan.FromMilliseconds(10);

        while (true)
        {
            TryClearScreen();
            System.Console.WriteLine("=== HelixForge Console Menu ===");
            System.Console.WriteLine();
            System.Console.WriteLine("  1. UAV stabilization demo (simulation)");
            System.Console.WriteLine("  2. UAV stabilization demo (hardware)");
            System.Console.WriteLine("  3. Magnetometer sample");
            System.Console.WriteLine("  4. Barometer sample");
            System.Console.WriteLine("  5. Servo sample");
            System.Console.WriteLine("  6. GPS sample");
            System.Console.WriteLine("  0. Exit");
            System.Console.WriteLine();
            System.Console.Write("Choose an option: ");

            string? input = System.Console.ReadLine();
            if (!int.TryParse(input?.Trim(), out int choice))
            {
                System.Console.WriteLine("Please enter a valid number.");
                System.Console.WriteLine("Press Enter to continue...");
                System.Console.ReadLine();
                continue;
            }

            if (choice == 0)
                return;

            if (choice is >= 1 and <= 6)
            {
                double seconds = PromptDuration();
                var duration = TimeSpan.FromSeconds(seconds);
                RunMenuChoice(choice, timeStep, duration);
                System.Console.WriteLine();
                System.Console.WriteLine("Press Enter to return to the menu...");
                System.Console.ReadLine();
            }
            else
            {
                System.Console.WriteLine("Unknown option.");
                System.Console.WriteLine("Press Enter to continue...");
                System.Console.ReadLine();
            }
        }
    }

    static void TryClearScreen()
    {
        try
        {
            System.Console.Clear();
        }
        catch (IOException)
        {
            // No attached console (e.g. piped input); clearing is a no-op.
        }
    }

    static double PromptDuration()
    {
        System.Console.Write("Duration in seconds (default 5): ");
        string? input = System.Console.ReadLine();
        if (double.TryParse(input?.Trim(), out double seconds) && seconds > 0)
            return seconds;
        return 5.0;
    }

    static void RunMenuChoice(int choice, TimeSpan timeStep, TimeSpan duration)
    {
        switch (choice)
        {
            case 1:
                using (var telemetry = MakeBus())
                    SimulationDemo.Run(telemetry, timeStep, duration);
                break;
            case 2:
                using (var telemetry = MakeBus())
                    HardwareDemo.Run(telemetry, timeStep, duration);
                break;
            case 3:
                MagSample.Run(timeStep, duration);
                break;
            case 4:
                BaroSample.Run(timeStep, duration);
                break;
            case 5:
                ServoSample.Run(timeStep, duration);
                break;
            case 6:
                GpsSample.Run(timeStep, duration);
                break;
        }
    }

    static TelemetryBus MakeBus()
    {
        var telemetry = new TelemetryBus();
        telemetry.AddSink(new ConsoleSink());
        return telemetry;
    }

    static void RunSample(string name, TimeSpan timeStep, TimeSpan duration)
    {
        switch (name.ToLowerInvariant())
        {
            case "mag":
                MagSample.Run(timeStep, duration);
                break;
            case "baro":
                BaroSample.Run(timeStep, duration);
                break;
            case "servo":
                ServoSample.Run(timeStep, duration);
                break;
            case "gps":
                GpsSample.Run(timeStep, duration);
                break;
            default:
                System.Console.WriteLine($"Unknown sample '{name}'. Choose from: mag, baro, servo, gps.");
                break;
        }
    }
}
