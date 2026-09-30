using System;
using System.Text;

namespace HelixForge.Instruments;

/// <summary>
/// Formatter that builds SCPI response blocks. Writes into caller-provided
/// byte buffers (netstandard2.0-safe, no stackalloc) and always appends the
/// standard line terminator.
/// </summary>
public static class ScpiFormatter
{
    /// <summary>Represents <c>"#0\r\n"</c> indefinite-length block header.</summary>
    public static readonly byte[] IndefiniteBlockHeader = new byte[] { 0x23, 0x30, 0x0D, 0x0A };

    /// <summary>Appends an ASCII string to the buffer.</summary>
    public static void Append(byte[] buffer, ref int offset, string text)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (text == null) throw new ArgumentNullException(nameof(text));

        int count = Encoding.ASCII.GetByteCount(text);
        if (offset + count > buffer.Length)
            throw new ArgumentException("Buffer is too small.", nameof(buffer));
        offset += Encoding.ASCII.GetBytes(text, 0, text.Length, buffer, offset);
    }

    /// <summary>Appends a CRLF line terminator.</summary>
    public static void AppendCrLf(byte[] buffer, ref int offset)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset + 2 > buffer.Length)
            throw new ArgumentException("Buffer is too small.", nameof(buffer));
        buffer[offset++] = 0x0D;
        buffer[offset++] = 0x0A;
    }

    /// <summary>
    /// Writes a decimal number into an ASCII response block followed by CRLF.
    /// </summary>
    /// <param name="buffer">Destination buffer.</param>
    /// <param name="offset">Reference offset updated to just past the written bytes.</param>
    /// <param name="value">The number to format.</param>
    public static void WriteNumber(byte[] buffer, ref int offset, double value)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        string text = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        Append(buffer, ref offset, text);
        AppendCrLf(buffer, ref offset);
    }
}
