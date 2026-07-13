using System;

namespace HelixForge.Telemetry;

/// <summary>
/// Telemetry sink that invokes a delegate for each event.
/// Useful for custom telemetry handling without creating a full sink implementation.
/// </summary>
public sealed class DelegateSink : ITelemetrySink
{
    private readonly Action<TelemetryEvent> _handler;

    /// <summary>
    /// Creates a new delegate sink.
    /// </summary>
    /// <param name="handler">The delegate to invoke for each telemetry event.</param>
    /// <exception cref="ArgumentNullException">Thrown when handler is null.</exception>
    public DelegateSink(Action<TelemetryEvent> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <inheritdoc/>
    public void OnEvent(in TelemetryEvent telemetryEvent)
    {
        _handler(telemetryEvent);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
