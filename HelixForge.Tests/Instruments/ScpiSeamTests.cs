using System.Text;
using HelixForge.Instruments;

namespace HelixForge.Tests.Instruments;

public class ScpiSeamTests
{
    [Fact]
    public void IdnQuery_ReturnsInstrumentIdentity()
    {
        var conn = new InMemoryInstrumentConnection();
        var scope = new SimOscilloscopeDevice("scope-01", new OscilloscopeSimConfig());
        scope.Initialize();
        var responder = new SimScpiInstrument(conn, scope);

        conn.Write(Encoding.ASCII.GetBytes("*IDN?\r\n"));
        responder.ProcessAvailable();

        string reply = Encoding.ASCII.GetString(conn.ReadAvailable());
        Assert.StartsWith("HelixForge", reply);
    }

    [Fact]
    public void WaveformQuery_ReturnsDefiniteBlock()
    {
        var conn = new InMemoryInstrumentConnection();
        var scope = new SimOscilloscopeDevice("scope-01", new OscilloscopeSimConfig());
        scope.Initialize();
        var responder = new SimScpiInstrument(conn, scope);

        conn.Write(Encoding.ASCII.GetBytes(":WAV:DATA?\r\n"));
        responder.ProcessAvailable();

        byte[] reply = conn.ReadAvailable();
        Assert.Equal('#', (char)reply[0]);
        Assert.Equal('4', (char)reply[1]); // 1024 samples * 4 bytes = 4096

        // Parse the definite-length header to check payload size
        int lengthBytes = reply[1] - (byte)'0';
        string lengthStr = Encoding.ASCII.GetString(reply, 2, lengthBytes);
        int payloadLength = int.Parse(lengthStr);
        Assert.Equal(4096, payloadLength);
    }

    [Fact]
    public void UnknownQuery_ReturnsError()
    {
        var conn = new InMemoryInstrumentConnection();
        var scope = new SimOscilloscopeDevice("scope-01", new OscilloscopeSimConfig());
        scope.Initialize();
        var responder = new SimScpiInstrument(conn, scope);

        conn.Write(Encoding.ASCII.GetBytes(":NOPE?\r\n"));
        responder.ProcessAvailable();

        string reply = Encoding.ASCII.GetString(conn.ReadAvailable());
        Assert.StartsWith("ERROR", reply);
    }

    [Fact]
    public void PartialQuery_NotProcessedUntilTerminated()
    {
        var conn = new InMemoryInstrumentConnection();
        var scope = new SimOscilloscopeDevice("scope-01", new OscilloscopeSimConfig());
        scope.Initialize();
        var responder = new SimScpiInstrument(conn, scope);

        conn.Write(Encoding.ASCII.GetBytes("*IDN?"));
        responder.ProcessAvailable();
        Assert.Empty(conn.ReadAvailable());

        conn.Write(Encoding.ASCII.GetBytes("\r\n"));
        responder.ProcessAvailable();
        Assert.NotEmpty(conn.ReadAvailable());
    }
}