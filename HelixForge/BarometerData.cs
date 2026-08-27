using System;

namespace HelixForge;

/// <summary>
/// Snapshot of barometer sensor data at a specific point in time.
/// </summary>
public readonly struct BarometerData : IEquatable<BarometerData>
{
    /// <summary>Atmospheric pressure in hectopascals (hPa).</summary>
    public double Pressure { get; }

    /// <summary>Barometric altitude in meters above sea level.</summary>
    public double Altitude { get; }

    /// <summary>Time of this reading.</summary>
    public TimeSpan Timestamp { get; }

    /// <summary>Creates a new barometer data snapshot.</summary>
    /// <param name="pressure">Atmospheric pressure in hPa.</param>
    /// <param name="altitude">Barometric altitude in meters.</param>
    /// <param name="timestamp">Time of this reading.</param>
    public BarometerData(double pressure, double altitude, TimeSpan timestamp)
    {
        Pressure = pressure;
        Altitude = altitude;
        Timestamp = timestamp;
    }

    /// <summary>An empty barometer reading with zero values.</summary>
    public static readonly BarometerData Empty = new BarometerData(0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public bool Equals(BarometerData other)
    {
        return Pressure.Equals(other.Pressure)
            && Altitude.Equals(other.Altitude)
            && Timestamp.Equals(other.Timestamp);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BarometerData other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Pressure.GetHashCode();
            hash = hash * 31 + Altitude.GetHashCode();
            hash = hash * 31 + Timestamp.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"BarometerData[Pressure={Pressure:F2}hPa, Altitude={Altitude:F2}m, T={Timestamp.TotalMilliseconds:F1}ms]";
}
