using System;

namespace HelixForge.Instruments;

/// <summary>
/// A parsed SCPI query. A query is a command ending in '?' that is expected
/// to produce a response, e.g. "*IDN?" or ":CHAN1:SAMPLE?".
/// </summary>
public readonly struct ScpiQuery
{
    /// <summary>Creates a parsed SCPI query.</summary>
    public ScpiQuery(string command, string? arguments)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Arguments = arguments;
    }

    /// <summary>Gets the normalized SCPI command (uppercase, no trailing '?').</summary>
    public string Command { get; }

    /// <summary>Gets the query arguments, or null when none were present.</summary>
    public string? Arguments { get; }

    /// <inheritdoc/>
    public override string ToString() => Arguments == null ? Command : $"{Command} {Arguments}";
}
