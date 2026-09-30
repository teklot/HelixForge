using System;
using System.Globalization;
using System.Text;

namespace HelixForge.Instruments;

/// <summary>
/// Wire-facing SCPI responder that bridges an <see cref="IInstrumentStreamConnection"/>
/// to an <see cref="IOscilloscopeDevice"/>. Parses CRLF-terminated SCPI queries and
/// writes IEEE-488.2 block-framed responses. Holds zero SignalFlux references by design;
/// interop is the wire protocol itself.
/// </summary>
public sealed class SimScpiInstrument : IDisposable
{
    private readonly IInstrumentStreamConnection _connection;
    private readonly IOscilloscopeDevice _oscilloscope;
    private readonly StringBuilder _pending = new StringBuilder();
    private bool _disposed;

    /// <summary>
    /// Creates a new SCPI responder bound to the given connection and backend oscilloscope.
    /// </summary>
    /// <param name="connection">The byte-stream connection to the controller.</param>
    /// <param name="oscilloscope">The oscilloscope serving waveform data.</param>
    public SimScpiInstrument(IInstrumentStreamConnection connection, IOscilloscopeDevice oscilloscope)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
    }

    /// <summary>
    /// Processes any pending request bytes on the connection, replying to each
    /// complete CRLF-terminated query. Never blocks.
    /// </summary>
    public void ProcessAvailable()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SimScpiInstrument));

        byte[] incoming = _connection.ReadAvailable();
        if (incoming == null || incoming.Length == 0)
            return;

        _pending.Append(Encoding.ASCII.GetString(incoming));
        while (TryConsumeLine(out string? line))
        {
            string response = Dispatch(line!);
            WriteResponse(response);
        }
    }

    private bool TryConsumeLine(out string? line)
    {
        int index = _pending.ToString().IndexOf("\r\n", StringComparison.Ordinal);
        if (index < 0)
        {
            line = null;
            return false;
        }

        line = _pending.ToString(0, index).Trim();
        _pending.Remove(0, index + 2);
        return true;
    }

    private string Dispatch(string query)
    {
        string normalized = NormalizeQuery(query);
        if (normalized == "*IDN?")
            return "HelixForge,SimScpiInstrument,1.0";

        if (normalized.StartsWith(":WAV:DATA?", StringComparison.Ordinal))
            return EncodeWaveformBlock(_oscilloscope.ReadCapture());

        return "ERROR:UNKNOWN_QUERY";
    }

    private string EncodeWaveformBlock(OscilloscopeData data)
    {
        int sampleCount = data.ChannelCount > 0 ? data.Channels[0].SampleCount : 0;
        int bytesPerSample = sizeof(float);
        int payloadLength = sampleCount * bytesPerSample;
        byte[] payload = new byte[payloadLength];

        for (int i = 0; i < sampleCount; i++)
        {
            float sample = (float)data.Channels[0].Samples[i];
            byte[] raw = BitConverter.GetBytes(sample);
            Buffer.BlockCopy(raw, 0, payload, i * bytesPerSample, bytesPerSample);
        }

        // IEEE-488.2 definite-length block: #<digit><length><CRLF><data><CRLF>
        string length = payloadLength.ToString(CultureInfo.InvariantCulture);
        byte[] header = Encoding.ASCII.GetBytes("#" + length.Length);
        byte[] lengthData = Encoding.ASCII.GetBytes(length);
        byte[] terminator = { 0x0D, 0x0A };

        using (var stream = new System.IO.MemoryStream())
        {
            stream.Write(header, 0, header.Length);
            stream.Write(lengthData, 0, lengthData.Length);
            stream.Write(terminator, 0, terminator.Length);
            stream.Write(payload, 0, payload.Length);
            stream.Write(terminator, 0, terminator.Length);
            return Encoding.ASCII.GetString(stream.ToArray());
        }
    }

    private void WriteResponse(string response)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(response);
        _connection.Write(bytes);
    }

    private static string NormalizeQuery(string query)
    {
        return query.Trim().ToUpperInvariant();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}