namespace HelixForge.Telemetry;

/// <summary>
/// Determines which events are discarded when a bounded telemetry buffer is full.
/// </summary>
public enum TelemetryDropPolicy
{
    /// <summary>Discard the oldest buffered event to make room for the incoming event.</summary>
    DropOldest = 0,

    /// <summary>Discard the incoming event and keep the buffered events.</summary>
    DropNewest = 1
}
