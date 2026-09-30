using System;
using System.Text;
using HelixForge.Instruments;

namespace HelixForge.Console;

/// <summary>
/// Instrument sample: exercises every instrument sim — the SCPI wire seam
/// (SimScpiInstrument over an in-memory byte stream), oscilloscope, DAQ, PLC,
/// bench power supply, and battery cycler.
/// </summary>
internal static class InstrumentsSample
{
    public static void Run(TimeSpan timeStep, TimeSpan duration)
    {
        System.Console.WriteLine("=== Instruments / SCPI Wire-Seam Sample ===");
        System.Console.WriteLine("Deterministic sims of lab and industrial gear; interop is byte-level only.");
        System.Console.WriteLine();

        ScpiSeam();
        Oscilloscope();
        DaqLoop(timeStep, duration);
        Plc();
        BenchPsu(timeStep, duration);
        BatteryCycler(timeStep, duration);
    }

    private static void ScpiSeam()
    {
        System.Console.WriteLine("--- 1. SCPI Wire Seam ---");
        System.Console.WriteLine("SimOscilloscopeDevice served through SimScpiInstrument over");
        System.Console.WriteLine("InMemoryInstrumentConnection. Queries arrive as CRLF-terminated bytes.");
        System.Console.WriteLine();

        var scope = new SimOscilloscopeDevice(
            "scope-01",
            new OscilloscopeSimConfig
            {
                ChannelCount = 2,
                SampleRate = 250000.0,
                Amplitude = 3.3,
                FrequencyHz = 1000.0,
                WaveformShape = OscilloscopeWaveformShape.Sine,
                NoiseAmplitude = 0.02,
                RandomSeed = 42
            });
        scope.Initialize();

        var connection = new InMemoryInstrumentConnection();
        var responder = new SimScpiInstrument(connection, scope);

        Issue(connection, responder, "*IDN?");
        Issue(connection, responder, ":WAV:DATA?");

        scope.Dispose();
        responder.Dispose();
        connection.Dispose();
    }

    private static void Oscilloscope()
    {
        System.Console.WriteLine("--- 2. Oscilloscope (direct capture) ---");
        var scope = new SimOscilloscopeDevice(
            "scope-02",
            new OscilloscopeSimConfig
            {
                ChannelCount = 2,
                SampleRate = 250000.0,
                Amplitude = 3.3,
                FrequencyHz = 1000.0,
                WaveformShape = OscilloscopeWaveformShape.Sine,
                NoiseAmplitude = 0.02,
                RandomSeed = 42
            });
        scope.Initialize();
        scope.Arm();

        var capture = scope.ReadCapture();
        System.Console.WriteLine($"Capture: {capture.ChannelCount}ch at {capture.SampleRate,8:F0} S/s, " +
            $"{capture.Channels[0].SampleCount} samples/ch");
        System.Console.WriteLine();
        System.Console.WriteLine("  channel   peak      rms      first 4 samples");
        foreach (var channel in capture.Channels)
        {
            double peak = 0.0, sumSquares = 0.0;
            foreach (double sample in channel.Samples)
            {
                peak = Math.Max(peak, Math.Abs(sample));
                sumSquares += sample * sample;
            }
            double rms = Math.Sqrt(sumSquares / channel.SampleCount);
            System.Console.WriteLine(
                $"  {channel.ChannelName,-8} {peak,7:F2}V  {rms,7:F2}V   " +
                $"[{channel.Samples[0],6:F3} {channel.Samples[1],6:F3} {channel.Samples[2],6:F3} {channel.Samples[3],6:F3}]");
        }
        System.Console.WriteLine();
        scope.Dispose();
    }

    private static void DaqLoop(TimeSpan timeStep, TimeSpan duration)
    {
        System.Console.WriteLine("--- 3. DAQ (streaming scans) ---");
        var daq = new SimDaqDevice(
            "daq-01",
            new DaqSimConfig { ChannelCount = 4, SampleRate = 1000.0, Amplitude = 1.0, NoiseAmplitude = 0.05, RandomSeed = 7 });
        daq.Initialize();

        int printEvery = Math.Max(1, (int)(TimeSpan.FromSeconds(0.5).Ticks / timeStep.Ticks));
        int step = 0;
        for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += timeStep, step++)
        {
            var scan = daq.ReadScan();
            if (step % printEvery == 0)
            {
                System.Console.Write($"[{currentTime.TotalSeconds,5:F2}s] ");
                foreach (var channel in scan.Channels)
                    System.Console.Write($"{channel.ChannelName}={channel.Voltage,6:F3}V ");
                System.Console.WriteLine();
            }
        }
        System.Console.WriteLine();
        daq.Dispose();
    }

    private static void Plc()
    {
        System.Console.WriteLine("--- 4. PLC (ladder rung scan) ---");
        var plc = new SimPlcDevice("plc-01", inputCount: 4, outputCount: 2);
        plc.Initialize();

        System.Console.WriteLine("  rung program: Q0 = I0 && I1,  Q1 = I2 || I3");
        System.Console.WriteLine("     inputs      scan  ->   outputs");
        int[][] scenarios =
        {
            new[] { 1, 1, 0, 0 },
            new[] { 0, 1, 1, 0 },
            new[] { 1, 1, 1, 1 }
        };
        foreach (var inputs in scenarios)
        {
            for (int i = 0; i < inputs.Length; i++)
                plc.SetInput(i, inputs[i] == 1);
            plc.ScanOnce();
            System.Console.WriteLine(
                $"  [{string.Join(",", inputs)}]      scan  ->  Q0={(plc.ReadOutput(0) ? 1 : 0)} Q1={(plc.ReadOutput(1) ? 1 : 0)}");
        }
        System.Console.WriteLine();
        plc.Dispose();
    }

    private static void BenchPsu(TimeSpan timeStep, TimeSpan duration)
    {
        System.Console.WriteLine("--- 5. Bench power supply (enable + slew ramp) ---");
        var psu = new SimBenchPsuDevice(
            "psu-01",
            new BenchPsuSimConfig { MaxVoltage = 30.0, LoadResistance = 10.0, VoltageStepPerClock = 0.5 });
        psu.Initialize();
        psu.SetVoltage(12.0);
        psu.SetOutputEnabled(true);

        System.Console.WriteLine($"  target {psu.TargetVoltage:F1}V, max {psu.MaxVoltage:F0}V, load {10.0:F0}\u03a9");
        System.Console.WriteLine("   time       V        I");
        int printEvery = Math.Max(1, (int)(TimeSpan.FromSeconds(0.5).Ticks / timeStep.Ticks));
        int step = 0;
        for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += timeStep, step++)
        {
            psu.UpdateClock();
            if (step % printEvery == 0)
            {
                System.Console.WriteLine(
                    $"  {currentTime.TotalSeconds,5:F2}s  {psu.OutputVoltage,7:F2}V  {psu.OutputCurrent,7:F3}A");
            }
        }
        System.Console.WriteLine();
        psu.Dispose();
    }

    private static void BatteryCycler(TimeSpan timeStep, TimeSpan duration)
    {
        System.Console.WriteLine("--- 6. Battery cycler (constant-current charge) ---");
        var cycler = new SimBatteryCyclerDevice(
            "cycler-01",
            new BatteryCyclerSimConfig { CapacityAh = 3.0, NominalVoltage = 3.7, TimeStepSeconds = 1.0, InitialChargeFraction = 0.5 });
        cycler.Initialize();
        cycler.SetCurrent(2.0);

        System.Console.WriteLine("  charging a 3.0 Ah cell at 2.0 A");
        System.Console.WriteLine("   time       SOC      cell V");
        int printEvery = Math.Max(1, (int)(TimeSpan.FromSeconds(0.5).Ticks / timeStep.Ticks));
        int step = 0;
        for (var currentTime = TimeSpan.Zero; currentTime < duration; currentTime += timeStep, step++)
        {
            cycler.UpdateClock();
            if (step % printEvery == 0)
            {
                System.Console.WriteLine(
                    $"  {currentTime.TotalSeconds,5:F2}s  {cycler.ChargeFraction,7:F4}  {cycler.CellVoltage,7:F3}V");
            }
        }
        System.Console.WriteLine();
        cycler.Dispose();
    }

    private static void Issue(InMemoryInstrumentConnection connection, SimScpiInstrument responder, string query)
    {
        connection.Write(Encoding.ASCII.GetBytes(query + "\r\n"));
        responder.ProcessAvailable();

        var reply = Encoding.ASCII.GetString(connection.ReadAvailable());
        string preview = reply.Length > 80 ? reply.Substring(0, 80) + "..." : reply;
        System.Console.WriteLine($"  SCPI {query} -> {preview}");
    }
}