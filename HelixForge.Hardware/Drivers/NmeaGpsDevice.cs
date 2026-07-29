using System;
using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace HelixForge.Hardware.Drivers;

/// <summary>
/// Real GPS device driver that parses NMEA 0183 sentences over a serial port.
/// Supports $GPGGA and $GPRMC sentences for position, altitude, and velocity.
/// </summary>
public sealed partial class NmeaGpsDevice : IGpsDevice
{
    [GeneratedRegex(@"^\$GPGGA,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,", RegexOptions.Compiled)]
    private static partial Regex GgaPrefixRegex();

    [GeneratedRegex(@"^\$GPRMC,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*", RegexOptions.Compiled)]
    private static partial Regex RmcPrefixRegex();

    private readonly string _portName;
    private readonly int _baudRate;
    private SerialPort? _serialPort;
    private GpsData _latestReading;

    /// <inheritdoc />
    public string DeviceId { get; }

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public GpsData LatestReading => _latestReading;

    /// <summary>Creates a new NMEA GPS device.</summary>
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

    /// <inheritdoc />
    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        _serialPort = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 100,
            NewLine = "\n"
        };
        _serialPort.Open();

        IsInitialized = true;
    }

    /// <inheritdoc />
    public GpsData Read()
    {
        if (!IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is not initialized.");

        while (_serialPort!.BytesToRead > 0)
        {
            try
            {
                string? line = _serialPort.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (GgaPrefixRegex().IsMatch(line))
                    TryParseGga(line);
                else if (RmcPrefixRegex().IsMatch(line))
                    TryParseRmc(line);
            }
            catch (TimeoutException)
            {
                break;
            }
        }

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
        _serialPort?.Close();
        _serialPort?.Dispose();
        _serialPort = null;
        IsInitialized = false;
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

        _latestReading = new GpsData(latitude, longitude, altitude, new Vector3(0, 0, 0), TimeSpan.Zero);
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
            new Vector3(speedMps, 0, 0), TimeSpan.Zero);
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
