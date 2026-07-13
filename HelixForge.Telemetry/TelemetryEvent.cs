using System;

namespace HelixForge.Telemetry;

/// <summary>
/// A telemetry data point produced by a device.
/// </summary>
public readonly struct TelemetryEvent : IEquatable<TelemetryEvent>
{
    /// <summary>The source device identifier.</summary>
    public string DeviceId { get; }

    /// <summary>The metric name (e.g., "orientation.x", "throttle").</summary>
    public string MetricName { get; }

    /// <summary>The scalar value of this event. Zero if this is a vector event.</summary>
    public double DoubleValue { get; }

    /// <summary>The vector value of this event. Zero if this is a scalar event.</summary>
    public Vector3 VectorValue { get; }

    /// <summary>Whether this event carries a vector value.</summary>
    public bool IsVector { get; }

    /// <summary>Time of the measurement.</summary>
    public TimeSpan Timestamp { get; }

    /// <summary>Creates a scalar telemetry event.</summary>
    public TelemetryEvent(string deviceId, string metricName, double value, TimeSpan timestamp)
    {
        DeviceId = deviceId;
        MetricName = metricName;
        DoubleValue = value;
        VectorValue = Vector3.Zero;
        IsVector = false;
        Timestamp = timestamp;
    }

    /// <summary>Creates a vector telemetry event.</summary>
    public TelemetryEvent(string deviceId, string metricName, in Vector3 value, TimeSpan timestamp)
    {
        DeviceId = deviceId;
        MetricName = metricName;
        DoubleValue = 0;
        VectorValue = value;
        IsVector = true;
        Timestamp = timestamp;
    }

    /// <inheritdoc />
    public bool Equals(TelemetryEvent other)
    {
        return DeviceId == other.DeviceId
            && MetricName == other.MetricName
            && DoubleValue.Equals(other.DoubleValue)
            && VectorValue.Equals(other.VectorValue)
            && IsVector == other.IsVector
            && Timestamp.Equals(other.Timestamp);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TelemetryEvent other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (DeviceId?.GetHashCode() ?? 0);
            hash = hash * 31 + (MetricName?.GetHashCode() ?? 0);
            hash = hash * 31 + DoubleValue.GetHashCode();
            hash = hash * 31 + VectorValue.GetHashCode();
            hash = hash * 31 + IsVector.GetHashCode();
            hash = hash * 31 + Timestamp.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (IsVector)
            return $"[{DeviceId}] {MetricName} = {VectorValue} @ {Timestamp.TotalMilliseconds:F1}ms";
        return $"[{DeviceId}] {MetricName} = {DoubleValue:F4} @ {Timestamp.TotalMilliseconds:F1}ms";
    }
}
