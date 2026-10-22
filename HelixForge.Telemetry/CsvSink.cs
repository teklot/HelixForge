using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace HelixForge.Telemetry;

/// <summary>
/// Telemetry sink that persists events to a CSV file using a tidy long-row layout.
/// Each row is <c>timestamp_ms,device_id,metric,value</c>. Vector events are expanded
/// into three rows with <c>.x</c>, <c>.y</c>, and <c>.z</c> metric suffixes.
/// Rows use invariant-culture formatting and <c>\n</c> line endings so output is
/// identical across platforms.
/// </summary>
public sealed class CsvSink : ITelemetrySink
{
    private const string Header = "timestamp_ms,device_id,metric,value\n";
    private static readonly char[] FieldsRequiringQuotes = { ',', '"', '\r', '\n' };

    private readonly TextWriter _writer;
    private readonly bool _leaveOpen;
    private readonly object _gate = new object();
    private long _rowsWritten;
    private bool _disposed;

    /// <summary>
    /// Creates a CSV sink that writes to the specified file, replacing any existing file.
    /// The file is created immediately with a header row.
    /// </summary>
    /// <param name="path">Destination file path.</param>
    public CsvSink(string path)
        : this(CreateFileWriter(path), false)
    {
    }

    /// <summary>
    /// Creates a CSV sink that writes to the specified text writer.
    /// The header row is written immediately.
    /// </summary>
    /// <param name="writer">The text writer to output to.</param>
    /// <param name="leaveOpen">Whether to leave the writer open on dispose.</param>
    public CsvSink(TextWriter writer, bool leaveOpen = false)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _leaveOpen = leaveOpen;
        _writer.Write(Header);
    }

    /// <summary>
    /// Gets the number of data rows written, counting each vector axis as a row.
    /// </summary>
    public long RowsWritten
    {
        get
        {
            lock (_gate)
                return _rowsWritten;
        }
    }

    /// <inheritdoc/>
    public void OnEvent(in TelemetryEvent telemetryEvent)
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            string timestamp = telemetryEvent.Timestamp.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture);
            string deviceId = Escape(telemetryEvent.DeviceId);
            string metricName = telemetryEvent.MetricName ?? string.Empty;

            if (telemetryEvent.IsVector)
            {
                WriteRow(timestamp, deviceId, metricName + ".x", telemetryEvent.VectorValue.X);
                WriteRow(timestamp, deviceId, metricName + ".y", telemetryEvent.VectorValue.Y);
                WriteRow(timestamp, deviceId, metricName + ".z", telemetryEvent.VectorValue.Z);
            }
            else
            {
                WriteRow(timestamp, deviceId, metricName, telemetryEvent.DoubleValue);
            }
        }
    }

    /// <summary>
    /// Flushes buffered rows to the underlying writer.
    /// </summary>
    public void Flush()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _writer.Flush();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _writer.Flush();
            if (!_leaveOpen)
                _writer.Dispose();
        }
    }

    private void WriteRow(string timestamp, string deviceId, string metricName, double value)
    {
        _writer.Write(timestamp);
        _writer.Write(',');
        _writer.Write(deviceId);
        _writer.Write(',');
        _writer.Write(Escape(metricName));
        _writer.Write(',');
        _writer.Write(value.ToString("R", CultureInfo.InvariantCulture));
        _writer.Write('\n');
        _rowsWritten++;
    }

    private static string Escape(string? field)
    {
        if (field == null || field.Length == 0)
            return string.Empty;

        if (field.IndexOfAny(FieldsRequiringQuotes) < 0)
            return field;

        return "\"" + field.Replace("\"", "\"\"") + "\"";
    }

    private static TextWriter CreateFileWriter(string path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        return new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
