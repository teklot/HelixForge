using System;

namespace HelixForge;

/// <summary>
/// Abstraction for publishing telemetry data from devices.
/// Implemented by <c>TelemetryBus</c> in HelixForge.Telemetry.
/// Devices accept this optionally to remain telemetry-agnostic at compile time.
/// </summary>
public interface ITelemetryPublisher
{
    /// <summary>
    /// Publishes a scalar telemetry value for a specific device and metric.
    /// </summary>
    /// <param name="deviceId">The source device identifier.</param>
    /// <param name="metricName">The metric name (e.g., "orientation.roll").</param>
    /// <param name="value">The measured value.</param>
    /// <param name="timestamp">The time of the measurement.</param>
    void Publish(string deviceId, string metricName, double value, TimeSpan timestamp);

    /// <summary>
    /// Publishes a 3D vector telemetry value for a specific device and metric.
    /// </summary>
    /// <param name="deviceId">The source device identifier.</param>
    /// <param name="metricName">The metric name (e.g., "acceleration").</param>
    /// <param name="value">The vector value.</param>
    /// <param name="timestamp">The time of the measurement.</param>
    void Publish(string deviceId, string metricName, in Vector3 value, TimeSpan timestamp);
}
