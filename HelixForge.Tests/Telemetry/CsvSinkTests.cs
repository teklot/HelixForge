using System;
using System.Globalization;
using System.IO;
using HelixForge.Telemetry;

namespace HelixForge.Tests.Telemetry;

public class CsvSinkTests
{
    private const string Header = "timestamp_ms,device_id,metric,value";

    [Fact]
    public void OnEvent_Scalar_WritesHeaderAndRow()
    {
        var writer = new StringWriter();
        using var sink = new CsvSink(writer, leaveOpen: true);

        sink.OnEvent(new TelemetryEvent("imu-01", "throttle", 0.5, TimeSpan.FromMilliseconds(12.5)));

        string[] lines = Lines(writer.ToString());
        Assert.Equal(Header, lines[0]);
        Assert.Equal("12.5,imu-01,throttle,0.5", lines[1]);
    }

    [Fact]
    public void OnEvent_Vector_ExpandsIntoThreeAxisRows()
    {
        var writer = new StringWriter();
        using var sink = new CsvSink(writer, leaveOpen: true);

        var acceleration = new Vector3(1.0, -2.5, 3.25);
        sink.OnEvent(new TelemetryEvent("imu-01", "acceleration", in acceleration, TimeSpan.FromSeconds(1)));

        string[] lines = Lines(writer.ToString());
        Assert.Equal("1000,imu-01,acceleration.x,1", lines[1]);
        Assert.Equal("1000,imu-01,acceleration.y,-2.5", lines[2]);
        Assert.Equal("1000,imu-01,acceleration.z,3.25", lines[3]);
    }

    [Fact]
    public void RowsWritten_CountsExpandedVectorRows()
    {
        var writer = new StringWriter();
        using var sink = new CsvSink(writer, leaveOpen: true);

        sink.OnEvent(new TelemetryEvent("imu-01", "throttle", 0.5, TimeSpan.Zero));
        var vector = new Vector3(1.0, 2.0, 3.0);
        sink.OnEvent(new TelemetryEvent("imu-01", "acceleration", in vector, TimeSpan.Zero));

        Assert.Equal(4, sink.RowsWritten);
    }

    [Fact]
    public void OnEvent_QuotesFieldsContainingDelimitersAndQuotes()
    {
        var writer = new StringWriter();
        using var sink = new CsvSink(writer, leaveOpen: true);

        sink.OnEvent(new TelemetryEvent("dev,01", "metric\"x", 1.0, TimeSpan.Zero));

        Assert.Equal(Header + "\n0,\"dev,01\",\"metric\"\"x\",1\n", writer.ToString());
    }

    [Fact]
    public void OnEvent_QuotesFieldsContainingNewlines()
    {
        var writer = new StringWriter();
        using var sink = new CsvSink(writer, leaveOpen: true);

        sink.OnEvent(new TelemetryEvent("dev", "line\nbreak", 1.0, TimeSpan.Zero));

        Assert.Equal(Header + "\n0,dev,\"line\nbreak\",1\n", writer.ToString());
    }

    [Fact]
    public void OnEvent_NullDeviceAndMetric_WritesEmptyFields()
    {
        var writer = new StringWriter();
        using var sink = new CsvSink(writer, leaveOpen: true);

        sink.OnEvent(new TelemetryEvent(null!, null!, 1.0, TimeSpan.Zero));

        Assert.Equal(Header + "\n0,,,1\n", writer.ToString());
    }

    [Fact]
    public void OnEvent_UsesInvariantCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var writer = new StringWriter();
            using var sink = new CsvSink(writer, leaveOpen: true);
            sink.OnEvent(new TelemetryEvent("dev", "metric", 1.5, TimeSpan.FromMilliseconds(0.25)));

            Assert.Equal(Header + "\n0.25,dev,metric,1.5\n", writer.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void OnEvent_AfterDispose_IsIgnored()
    {
        var writer = new StringWriter();
        var sink = new CsvSink(writer, leaveOpen: true);
        sink.Dispose();

        sink.OnEvent(new TelemetryEvent("dev", "metric", 1.0, TimeSpan.Zero));

        Assert.Equal(Header + "\n", writer.ToString());
        Assert.Equal(0, sink.RowsWritten);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var writer = new StringWriter();
        var sink = new CsvSink(writer, leaveOpen: true);

        sink.Dispose();
        sink.Dispose();

        Assert.Equal(Header + "\n", writer.ToString());
    }

    [Fact]
    public void FileConstructor_TruncatesExistingFileAndWritesLfEndings()
    {
        string path = TempPath();
        try
        {
            File.WriteAllText(path, "stale content");

            using (var sink = new CsvSink(path))
            {
                sink.OnEvent(new TelemetryEvent("dev", "x", 1.0, TimeSpan.FromMilliseconds(1)));
            }

            string content = File.ReadAllText(path);
            Assert.Equal(Header + "\n1,dev,x,1\n", content);
            Assert.DoesNotContain("\r", content);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Flush_WritesBufferedRowsBeforeDispose()
    {
        string path = TempPath();
        try
        {
            using var sink = new CsvSink(path);
            sink.OnEvent(new TelemetryEvent("dev", "x", 2.0, TimeSpan.FromMilliseconds(3)));

            sink.Flush();

            Assert.Contains("3,dev,x,2", ReadShared(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string TempPath()
    {
        return Path.Combine(Path.GetTempPath(), "helixforge-csv-" + Guid.NewGuid().ToString("N") + ".csv");
    }

    private static string[] Lines(string content)
    {
        return content.TrimEnd('\n').Split('\n');
    }
}
