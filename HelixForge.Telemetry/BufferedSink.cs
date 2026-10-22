using System;
using System.Diagnostics;
using System.Threading;

namespace HelixForge.Telemetry;

/// <summary>
/// Buffered telemetry sink that hands events to an inner sink on a dedicated background thread.
/// Events are queued in a fixed-size ring buffer so that a slow consumer never blocks the
/// producer; when the buffer is full, the configured <see cref="TelemetryDropPolicy"/>
/// decides which events are discarded. This enforces the HelixForge rule that telemetry
/// never affects system behavior.
/// </summary>
public sealed class BufferedSink : ITelemetrySink
{
    private static readonly TimeSpan DisposeTimeout = TimeSpan.FromSeconds(5);

    private readonly ITelemetrySink _innerSink;
    private readonly TelemetryEvent[] _buffer;
    private readonly object _gate = new object();
    private readonly Thread _worker;
    private int _head;
    private int _count;
    private long _enqueued;
    private long _resolved;
    private long _dropped;
    private long _failed;
    private bool _stopping;
    private bool _disposed;

    /// <summary>
    /// Creates a buffered sink around the specified inner sink and starts its background worker.
    /// </summary>
    /// <param name="innerSink">The sink that receives events on the background thread.</param>
    /// <param name="capacity">Maximum number of queued events. Must be greater than zero.</param>
    /// <param name="dropPolicy">Policy applied when the queue is full.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="innerSink"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is not positive.</exception>
    public BufferedSink(
        ITelemetrySink innerSink,
        int capacity = 8192,
        TelemetryDropPolicy dropPolicy = TelemetryDropPolicy.DropOldest)
    {
        if (innerSink == null)
            throw new ArgumentNullException(nameof(innerSink));
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be greater than zero.");

        _innerSink = innerSink;
        _buffer = new TelemetryEvent[capacity];
        DropPolicy = dropPolicy;
        _worker = new Thread(ProcessQueue)
        {
            IsBackground = true,
            Name = "HelixForge.Telemetry.BufferedSink"
        };
        _worker.Start();
    }

    /// <summary>Gets the maximum number of queued events.</summary>
    public int Capacity => _buffer.Length;

    /// <summary>Gets the drop policy applied when the queue is full.</summary>
    public TelemetryDropPolicy DropPolicy { get; }

    /// <summary>Gets the number of queued events not yet handed to the inner sink.</summary>
    public int PendingCount
    {
        get
        {
            lock (_gate)
                return _count;
        }
    }

    /// <summary>Gets the number of events discarded because the queue was full.</summary>
    public long DroppedCount
    {
        get
        {
            lock (_gate)
                return _dropped;
        }
    }

    /// <summary>Gets the number of events the inner sink failed to accept.</summary>
    public long FailedCount => Interlocked.Read(ref _failed);

    /// <inheritdoc/>
    public void OnEvent(in TelemetryEvent telemetryEvent)
    {
        lock (_gate)
        {
            if (_stopping)
                return;

            if (_count == _buffer.Length)
            {
                _dropped++;
                if (DropPolicy == TelemetryDropPolicy.DropNewest)
                    return;

                _head = (_head + 1) % _buffer.Length;
                _count--;
                _resolved++;
            }

            int tail = (_head + _count) % _buffer.Length;
            _buffer[tail] = telemetryEvent;
            _count++;
            _enqueued++;
            Monitor.Pulse(_gate);
        }
    }

    /// <summary>
    /// Blocks until every event accepted before the call has been handed to the inner sink.
    /// </summary>
    public void Flush()
    {
        Flush(Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Blocks until every event accepted before the call has been handed to the inner sink,
    /// or until the timeout elapses.
    /// </summary>
    /// <param name="timeout">Maximum time to wait; use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
    /// <returns><c>true</c> if the queue drained; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is negative and not infinite.</exception>
    public bool Flush(TimeSpan timeout)
    {
        if (timeout < Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must not be negative.");

        lock (_gate)
        {
            long target = _enqueued;
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                while (_resolved < target)
                    Monitor.Wait(_gate);
                return true;
            }

            long start = Stopwatch.GetTimestamp();
            long timeoutTicks = (long)(timeout.TotalSeconds * Stopwatch.Frequency);
            while (_resolved < target)
            {
                long remainingTicks = timeoutTicks - (Stopwatch.GetTimestamp() - start);
                if (remainingTicks <= 0)
                    return false;

                Monitor.Wait(_gate, TimeSpan.FromSeconds((double)remainingTicks / Stopwatch.Frequency));
            }

            return true;
        }
    }

    /// <summary>
    /// Stops the background worker after draining accepted events, then disposes the inner sink.
    /// Events published after disposal are ignored.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _stopping = true;
            Monitor.PulseAll(_gate);
        }

        _worker.Join(DisposeTimeout);
        _innerSink.Dispose();
    }

    private void ProcessQueue()
    {
        while (true)
        {
            TelemetryEvent telemetryEvent;
            lock (_gate)
            {
                while (_count == 0 && !_stopping)
                    Monitor.Wait(_gate);

                if (_count == 0)
                    return;

                telemetryEvent = _buffer[_head];
                _head = (_head + 1) % _buffer.Length;
                _count--;
            }

            try
            {
                _innerSink.OnEvent(in telemetryEvent);
            }
            catch
            {
                Interlocked.Increment(ref _failed);
            }

            lock (_gate)
            {
                _resolved++;
                Monitor.PulseAll(_gate);
            }
        }
    }
}
