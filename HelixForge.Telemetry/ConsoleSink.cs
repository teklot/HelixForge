using System;
using System.Collections.Generic;
using System.IO;

namespace HelixForge.Telemetry;

/// <summary>
/// Telemetry sink that writes events to the console (stdout).
/// </summary>
public sealed class ConsoleSink : ITelemetrySink
{
    private readonly TextWriter _writer;
    private readonly bool _leaveOpen;

    /// <summary>
    /// Creates a new console sink writing to stdout.
    /// </summary>
    public ConsoleSink() : this(Console.Out, true)
    {
    }

    /// <summary>
    /// Creates a new console sink writing to the specified text writer.
    /// </summary>
    /// <param name="writer">The text writer to output to.</param>
    /// <param name="leaveOpen">Whether to leave the writer open on dispose.</param>
    public ConsoleSink(TextWriter writer, bool leaveOpen = false)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _leaveOpen = leaveOpen;
    }

    /// <inheritdoc/>
    public void OnEvent(in TelemetryEvent telemetryEvent)
    {
        _writer.WriteLine(telemetryEvent.ToString());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_leaveOpen)
            _writer.Dispose();
    }
}
