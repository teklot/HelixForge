using HelixForge;
using HelixForge.Simulation;
using HelixForge.Telemetry;

namespace HelixForge.Tests.Telemetry;

public class TelemetryBusTests
{
    [Fact]
    public void AddSink_IncreasesSinkCount()
    {
        var bus = new TelemetryBus();
        bus.AddSink(new DelegateSink(_ => { }));

        Assert.Equal(1, bus.SinkCount);

        bus.Dispose();
    }

    [Fact]
    public void AddSink_Null_Throws()
    {
        var bus = new TelemetryBus();
        Assert.Throws<ArgumentNullException>(() => bus.AddSink(null!));
        bus.Dispose();
    }

    [Fact]
    public void Publish_Scalar_DeliversToSinks()
    {
        var bus = new TelemetryBus();
        TelemetryEvent? received = null;
        bus.AddSink(new DelegateSink(e => received = e));

        bus.Publish("imu-01", "orientation.x", 1.5, TimeSpan.FromSeconds(1));

        Assert.NotNull(received);
        Assert.Equal("imu-01", received!.Value.DeviceId);
        Assert.Equal("orientation.x", received.Value.MetricName);
        Assert.Equal(1.5, received.Value.DoubleValue);
        Assert.False(received.Value.IsVector);

        bus.Dispose();
    }

    [Fact]
    public void Publish_Vector_DeliversToSinks()
    {
        var bus = new TelemetryBus();
        TelemetryEvent? received = null;
        bus.AddSink(new DelegateSink(e => received = e));

        var vec = new Vector3(1.0, 2.0, 3.0);
        bus.Publish("imu-01", "acceleration", in vec, TimeSpan.FromSeconds(1));

        Assert.NotNull(received);
        Assert.True(received!.Value.IsVector);
        Assert.Equal(new Vector3(1.0, 2.0, 3.0), received.Value.VectorValue);

        bus.Dispose();
    }

    [Fact]
    public void Publish_DeliversToMultipleSinks()
    {
        var bus = new TelemetryBus();
        int count = 0;
        bus.AddSink(new DelegateSink(_ => count++));
        bus.AddSink(new DelegateSink(_ => count++));

        bus.Publish("imu-01", "test", 1.0, TimeSpan.Zero);

        Assert.Equal(2, count);

        bus.Dispose();
    }

    [Fact]
    public void RemoveSink_StopsDelivery()
    {
        var bus = new TelemetryBus();
        int count = 0;
        var sink = new DelegateSink(_ => count++);
        bus.AddSink(sink);

        bus.Publish("imu-01", "test", 1.0, TimeSpan.Zero);
        Assert.Equal(1, count);

        bus.RemoveSink(sink);
        bus.Publish("imu-01", "test", 1.0, TimeSpan.Zero);
        Assert.Equal(1, count);

        bus.Dispose();
    }

    [Fact]
    public void Dispose_DisposesAllSinks()
    {
        var bus = new TelemetryBus();
        bus.AddSink(new DelegateSink(_ => { }) );
        // DelegateSink.Dispose is a no-op, so we just verify no exception

        bus.Dispose();

        // Publishing after dispose should not throw
        bus.Publish("imu-01", "test", 1.0, TimeSpan.Zero);
    }
}
