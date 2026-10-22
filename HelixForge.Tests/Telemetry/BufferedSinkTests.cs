using System;
using System.Collections.Generic;
using System.Threading;
using HelixForge.Telemetry;

namespace HelixForge.Tests.Telemetry;

public class BufferedSinkTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void OnEvent_ForwardsEventsInOrder()
    {
        var inner = new RecordingSink();
        using var sink = new BufferedSink(inner, capacity: 16);

        for (int i = 0; i < 10; i++)
            sink.OnEvent(Scalar(i));

        Assert.True(sink.Flush(Timeout));
        Assert.Equal(new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, inner.Values);
    }

    [Fact]
    public void FullQueue_DropNewest_KeepsOldestEvents()
    {
        var inner = new RecordingSink { Gate = new ManualResetEventSlim(false) };
        using var sink = new BufferedSink(inner, capacity: 4, TelemetryDropPolicy.DropNewest);

        sink.OnEvent(Scalar(0));
        Assert.True(inner.Started.Wait(Timeout, TestContext.Current.CancellationToken));

        for (int i = 1; i <= 10; i++)
            sink.OnEvent(Scalar(i));

        Assert.Equal(4, sink.PendingCount);
        Assert.Equal(6, sink.DroppedCount);

        inner.Gate.Set();
        Assert.True(sink.Flush(Timeout));
        Assert.Equal(new double[] { 0, 1, 2, 3, 4 }, inner.Values);
    }

    [Fact]
    public void FullQueue_DropOldest_KeepsNewestEvents()
    {
        var inner = new RecordingSink { Gate = new ManualResetEventSlim(false) };
        using var sink = new BufferedSink(inner, capacity: 4, TelemetryDropPolicy.DropOldest);

        sink.OnEvent(Scalar(0));
        Assert.True(inner.Started.Wait(Timeout, TestContext.Current.CancellationToken));

        for (int i = 1; i <= 10; i++)
            sink.OnEvent(Scalar(i));

        Assert.Equal(4, sink.PendingCount);
        Assert.Equal(6, sink.DroppedCount);

        inner.Gate.Set();
        Assert.True(sink.Flush(Timeout));
        Assert.Equal(new double[] { 0, 7, 8, 9, 10 }, inner.Values);
    }

    [Fact]
    public void Flush_ZeroTimeout_ReturnsFalseWhilePending()
    {
        var inner = new RecordingSink { Gate = new ManualResetEventSlim(false) };
        using var sink = new BufferedSink(inner, capacity: 8);

        sink.OnEvent(Scalar(1));
        Assert.True(inner.Started.Wait(Timeout, TestContext.Current.CancellationToken));

        Assert.False(sink.Flush(TimeSpan.Zero));

        inner.Gate.Set();
        Assert.True(sink.Flush(Timeout));
    }

    [Fact]
    public void Dispose_DrainsQueuedEventsAndDisposesInnerSink()
    {
        var inner = new RecordingSink();
        var sink = new BufferedSink(inner, capacity: 1024);

        for (int i = 0; i < 100; i++)
            sink.OnEvent(Scalar(i));

        sink.Dispose();

        Assert.Equal(100, inner.Count);
        Assert.True(inner.Disposed);
        Assert.Equal(0, sink.PendingCount);
    }

    [Fact]
    public void OnEvent_AfterDispose_IsIgnored()
    {
        var inner = new RecordingSink();
        var sink = new BufferedSink(inner, capacity: 8);
        sink.Dispose();

        sink.OnEvent(Scalar(1));

        Assert.Equal(0, inner.Count);
        Assert.Equal(0, sink.DroppedCount);
    }

    [Fact]
    public void InnerSinkThrows_CountsFailureWithoutCrashing()
    {
        var inner = new RecordingSink { Throw = true };
        using var sink = new BufferedSink(inner, capacity: 8);

        for (int i = 0; i < 3; i++)
            sink.OnEvent(Scalar(i));

        Assert.True(sink.Flush(Timeout));
        Assert.Equal(3, sink.FailedCount);
        Assert.Equal(0, inner.Count);
    }

    [Fact]
    public void Constructor_RejectsInvalidArguments()
    {
        var inner = new RecordingSink();

        Assert.Throws<ArgumentNullException>(() => new BufferedSink(null!, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BufferedSink(inner, 0));
    }

    [Fact]
    public void Flush_RejectsNegativeTimeout()
    {
        var inner = new RecordingSink();
        using var sink = new BufferedSink(inner, capacity: 8);

        Assert.Throws<ArgumentOutOfRangeException>(() => sink.Flush(TimeSpan.FromSeconds(-2)));
    }

    [Fact]
    public void ConcurrentProducers_AccountForEveryEvent()
    {
        const int producerCount = 4;
        const int eventsPerProducer = 1000;
        var inner = new RecordingSink();
        using var sink = new BufferedSink(inner, capacity: 256, TelemetryDropPolicy.DropOldest);

        var producers = new Thread[producerCount];
        for (int p = 0; p < producerCount; p++)
        {
            producers[p] = new Thread(() =>
            {
                for (int i = 0; i < eventsPerProducer; i++)
                    sink.OnEvent(Scalar(i));
            });
            producers[p].Start();
        }

        foreach (var producer in producers)
            producer.Join();

        Assert.True(sink.Flush(TimeSpan.FromSeconds(30)));
        Assert.Equal(0, sink.PendingCount);
        Assert.Equal(producerCount * eventsPerProducer, inner.Count + sink.DroppedCount);
    }

    private static TelemetryEvent Scalar(int value)
    {
        return new TelemetryEvent("dev", "metric", value, TimeSpan.Zero);
    }

    private sealed class RecordingSink : ITelemetrySink
    {
        private readonly object _gate = new object();
        private readonly List<double> _values = new List<double>();

        public ManualResetEventSlim Started { get; } = new ManualResetEventSlim(false);

        public ManualResetEventSlim? Gate { get; set; }

        public bool Throw { get; set; }

        public bool Disposed { get; private set; }

        public int Count
        {
            get
            {
                lock (_gate)
                    return _values.Count;
            }
        }

        public double[] Values
        {
            get
            {
                lock (_gate)
                    return _values.ToArray();
            }
        }

        public void OnEvent(in TelemetryEvent telemetryEvent)
        {
            Started.Set();
            Gate?.Wait();
            if (Throw)
                throw new InvalidOperationException("Simulated sink failure.");

            lock (_gate)
                _values.Add(telemetryEvent.DoubleValue);
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
