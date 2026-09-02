using System;

namespace HelixForge.Console;

/// <summary>
/// Parsed command-line options for the HelixForge console demos.
/// </summary>
internal sealed class CliOptions
{
    /// <summary>Selected sample name, or null to run the golden-path demo.</summary>
    public string? SampleName { get; private set; }

    /// <summary>True when real hardware (not simulation) is requested.</summary>
    public bool UseRealHardware { get; private set; }

    /// <summary>Duration of the demo run in seconds. Default is 5.</summary>
    public double DurationSeconds { get; private set; } = 5.0;

    /// <summary>True when usage help was requested.</summary>
    public bool ShowHelp { get; private set; }

    /// <summary>Time step for the simulation. Default is 10ms (100Hz).</summary>
    public TimeSpan TimeStep { get; } = TimeSpan.FromMilliseconds(10);

    private CliOptions() { }

    /// <summary>
    /// Parses command-line arguments into a <see cref="CliOptions"/>.
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    /// <returns>The parsed options.</returns>
    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                case "--sample":
                    if (i + 1 < args.Length)
                        options.SampleName = args[++i];
                    break;
                case "--mode":
                    if (i + 1 < args.Length)
                        options.UseRealHardware = string.Equals(args[++i], "real", StringComparison.OrdinalIgnoreCase);
                    break;
                case "--duration":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out double seconds) && seconds > 0)
                        options.DurationSeconds = seconds;
                    break;
            }
        }

        return options;
    }

    /// <summary>Prints usage help to the console.</summary>
    public static void PrintHelp()
    {
        System.Console.WriteLine("HelixForge console demos");
        System.Console.WriteLine();
        System.Console.WriteLine("Usage:");
        System.Console.WriteLine("  HelixForge.Console [options]");
        System.Console.WriteLine();
        System.Console.WriteLine("Options:");
        System.Console.WriteLine("  (no arguments)                 Show the interactive options menu.");
        System.Console.WriteLine("  --sample <mag|baro|servo|gps>  Run a per-device sample.");
        System.Console.WriteLine("  --mode <sim|real>              Run the golden-path demo in simulation (default) or on hardware.");
        System.Console.WriteLine("  --duration <seconds>           Length of the run (demo or sample). Default is 5.");
        System.Console.WriteLine("  --help, -h                     Show this help.");
    }
}
