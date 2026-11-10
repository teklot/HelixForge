using System;
using System.Collections.Generic;
using System.Threading;

namespace HelixForge.Telemetry;

/// <summary>
/// Central telemetry bus that routes events to registered sinks.
/// Implements <see cref="ITelemetryPublisher"/> to integrate with the simulation engine.
/// </summary>
public sealed class TelemetryBus : ITelemetryPublisher, IDisposable
{
    private readonly List<ITelemetrySink> _sinks = new List<ITelemetrySink>();
    private long _failedPublishCount;
    private Exception? _lastFailure;
    private bool _disposed;

    /// <summary>
    /// Gets the number of registered sinks.
    /// </summary>
    public int SinkCount => _sinks.Count;

    /// <summary>
    /// Gets the number of sink callbacks that threw while handling telemetry.
    /// A failing sink never prevents delivery to the remaining sinks.
    /// </summary>
    public long FailedPublishCount => Interlocked.Read(ref _failedPublishCount);

    /// <summary>
    /// Gets the most recent exception thrown by a sink, or <c>null</c> if no sink has failed.
    /// </summary>
    public Exception? LastFailure => Volatile.Read(ref _lastFailure);

    /// <summary>
    /// Registers a telemetry sink to receive events.
    /// </summary>
    /// <param name="sink">The sink to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when sink is null.</exception>
    public void AddSink(ITelemetrySink sink)
    {
        if (sink == null)
            throw new ArgumentNullException(nameof(sink));
        _sinks.Add(sink);
    }

    /// <summary>
    /// Removes a telemetry sink.
    /// </summary>
    /// <param name="sink">The sink to remove.</param>
    /// <returns>True if the sink was found and removed.</returns>
    public bool RemoveSink(ITelemetrySink sink)
    {
        return _sinks.Remove(sink);
    }

    /// <summary>
    /// Publishes a scalar telemetry value to all registered sinks.
    /// </summary>
    public void Publish(string deviceId, string metricName, double value, TimeSpan timestamp)
    {
        if (_disposed)
            return;

        var telemetryEvent = new TelemetryEvent(deviceId, metricName, value, timestamp);
        for (int i = 0; i < _sinks.Count; i++)
        {
            try
            {
                _sinks[i].OnEvent(in telemetryEvent);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedPublishCount);
                Volatile.Write(ref _lastFailure, ex);
            }
        }
    }

    /// <summary>
    /// Publishes a vector telemetry value to all registered sinks.
    /// </summary>
    public void Publish(string deviceId, string metricName, in Vector3 value, TimeSpan timestamp)
    {
        if (_disposed)
            return;

        var telemetryEvent = new TelemetryEvent(deviceId, metricName, in value, timestamp);
        for (int i = 0; i < _sinks.Count; i++)
        {
            try
            {
                _sinks[i].OnEvent(in telemetryEvent);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedPublishCount);
                Volatile.Write(ref _lastFailure, ex);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        for (int i = 0; i < _sinks.Count; i++)
        {
            _sinks[i].Dispose();
        }
        _sinks.Clear();
    }
}
