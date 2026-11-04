using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HelixForge.Hardware.Transports;

namespace HelixForge.Hardware.Drivers;

/// <summary>
/// Real GPS device driver that parses NMEA 0183 sentences over a serial transport.
/// Supports $--GGA and $--RMC sentences for position, altitude, and velocity.
/// </summary>
public sealed partial class NmeaGpsDevice : IGpsDevice
{
    private const int MaxPendingLength = 4096;

    [GeneratedRegex(@"^\$[A-Z]{2}GGA,", RegexOptions.Compiled)]
    private static partial Regex GgaPrefixRegex();

    [GeneratedRegex(@"^\$[A-Z]{2}RMC,", RegexOptions.Compiled)]
    private static partial Regex RmcPrefixRegex();

    private readonly string _portName = string.Empty;
    private readonly int _baudRate;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private ISerialTransport? _transport;
    private string _pending = string.Empty;
    private GpsData _latestReading;

    /// <inheritdoc />
    public string DeviceId { get; }

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public GpsData LatestReading => _latestReading;

    /// <summary>Creates a new NMEA GPS device on a physical serial port.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="portName">Serial port name (e.g., "COM3" or "/dev/ttyAMA0").</param>
    /// <param name="baudRate">Baud rate (typically 9600 or 115200).</param>
    public NmeaGpsDevice(string deviceId, string portName, int baudRate)
    {
        DeviceId = deviceId;
        _portName = portName;
        _baudRate = baudRate;
        _latestReading = GpsData.Empty;
    }

    /// <summary>Creates a new NMEA GPS device on a supplied serial transport.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="transport">Serial transport. The driver takes ownership of it.</param>
    public NmeaGpsDevice(string deviceId, ISerialTransport transport)
    {
        DeviceId = deviceId;
        _transport = transport;
        _latestReading = GpsData.Empty;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        ISerialTransport transport = _transport ?? (_transport = new SerialPortTransport(_portName, _baudRate));
        if (!transport.IsOpen)
            transport.Open();

        _pending = string.Empty;
        _stopwatch.Restart();
        IsInitialized = true;
    }

    /// <inheritdoc />
    public GpsData Read()
    {
        if (!IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is not initialized.");

        if (_transport != null && _transport.BytesToRead > 0)
            ProcessIncoming(_transport.ReadAvailable());

        return _latestReading;
    }

    /// <inheritdoc />
    public void Update(TimeSpan deltaTime)
    {
        Read();
    }

    /// <inheritdoc />
    public void Reset()
    {
        Dispose();
        _latestReading = GpsData.Empty;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopwatch.Reset();
        _transport?.Dispose();
        _transport = null;
        _pending = string.Empty;
        IsInitialized = false;
    }

    private void ProcessIncoming(byte[] bytes)
    {
        if (bytes.Length == 0)
            return;

        _pending += Encoding.ASCII.GetString(bytes);

        int newlineIndex;
        while ((newlineIndex = _pending.IndexOf('\n')) >= 0)
        {
            string line = _pending.Substring(0, newlineIndex).Trim();
            _pending = _pending.Substring(newlineIndex + 1);

            if (line.Length == 0)
                continue;

            if (GgaPrefixRegex().IsMatch(line))
                TryParseGga(line);
            else if (RmcPrefixRegex().IsMatch(line))
                TryParseRmc(line);
        }

        if (_pending.Length > MaxPendingLength)
            _pending = string.Empty;
    }

    private void TryParseGga(string sentence)
    {
        string[] parts = sentence.Split(',');
        if (parts.Length < 10)
            return;

        if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double latRaw))
            return;
        if (!double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double lonRaw))
            return;
        if (!double.TryParse(parts[9], NumberStyles.Float, CultureInfo.InvariantCulture, out double altitude))
            return;

        double latitude = ConvertNmeaCoordinate(latRaw, parts[3]);
        double longitude = ConvertNmeaCoordinate(lonRaw, parts[5]);

        _latestReading = new GpsData(latitude, longitude, altitude, _latestReading.Velocity, _stopwatch.Elapsed);
    }

    private void TryParseRmc(string sentence)
    {
        string[] parts = sentence.Split(',');
        if (parts.Length < 8)
            return;

        if (!double.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out double speedKnots))
            return;

        double speedMps = speedKnots * 0.514444;

        var current = _latestReading;
        _latestReading = new GpsData(current.Latitude, current.Longitude, current.Altitude,
            new Vector3(speedMps, 0, 0), _stopwatch.Elapsed, current.FixStatus);
    }

    private static double ConvertNmeaCoordinate(double raw, string direction)
    {
        int degrees = (int)(raw / 100);
        double minutes = raw - degrees * 100;
        double result = degrees + minutes / 60.0;

        if (direction is "S" or "W")
            result = -result;

        return result;
    }
}
