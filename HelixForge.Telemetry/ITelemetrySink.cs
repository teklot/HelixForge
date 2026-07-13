namespace HelixForge.Telemetry;

/// <summary>
/// Interface for telemetry sinks that receive telemetry events.
/// Follows the Observer pattern to decouple producers from consumers.
/// </summary>
public interface ITelemetrySink : System.IDisposable
{
    /// <summary>
    /// Called when a telemetry event is published.
    /// </summary>
    /// <param name="telemetryEvent">The telemetry event data.</param>
    void OnEvent(in TelemetryEvent telemetryEvent);
}
