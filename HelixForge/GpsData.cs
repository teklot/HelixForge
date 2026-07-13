using System;

namespace HelixForge;

/// <summary>
/// Snapshot of GPS receiver data at a specific point in time.
/// </summary>
public readonly struct GpsData : IEquatable<GpsData>
{
    /// <summary>Latitude in decimal degrees (positive = North).</summary>
    public double Latitude { get; }

    /// <summary>Longitude in decimal degrees (positive = East).</summary>
    public double Longitude { get; }

    /// <summary>Altitude above mean sea level in meters.</summary>
    public double Altitude { get; }

    /// <summary>3D velocity vector in meters per second.</summary>
    public Vector3 Velocity { get; }

    /// <summary>Time of this reading.</summary>
    public TimeSpan Timestamp { get; }

    /// <summary>Creates a new GPS data snapshot.</summary>
    /// <param name="latitude">Latitude in decimal degrees.</param>
    /// <param name="longitude">Longitude in decimal degrees.</param>
    /// <param name="altitude">Altitude in meters.</param>
    /// <param name="velocity">Velocity vector in meters per second.</param>
    /// <param name="timestamp">Time of this reading.</param>
    public GpsData(double latitude, double longitude, double altitude, Vector3 velocity, TimeSpan timestamp)
    {
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
        Velocity = velocity;
        Timestamp = timestamp;
    }

    /// <summary>An empty GPS reading with zero values.</summary>
    public static readonly GpsData Empty = new GpsData(0, 0, 0, Vector3.Zero, TimeSpan.Zero);

    /// <inheritdoc />
    public bool Equals(GpsData other)
    {
        return Latitude.Equals(other.Latitude)
            && Longitude.Equals(other.Longitude)
            && Altitude.Equals(other.Altitude)
            && Velocity.Equals(other.Velocity)
            && Timestamp.Equals(other.Timestamp);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GpsData other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Latitude.GetHashCode();
            hash = hash * 31 + Longitude.GetHashCode();
            hash = hash * 31 + Altitude.GetHashCode();
            hash = hash * 31 + Velocity.GetHashCode();
            hash = hash * 31 + Timestamp.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"GpsData[Lat={Latitude:F6}, Lon={Longitude:F6}, Alt={Altitude:F2}m, T={Timestamp.TotalMilliseconds:F1}ms]";
}
