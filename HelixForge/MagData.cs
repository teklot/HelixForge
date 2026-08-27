using System;

namespace HelixForge;

/// <summary>
/// Snapshot of magnetometer sensor data at a specific point in time.
/// </summary>
public readonly struct MagData : IEquatable<MagData>
{
    /// <summary>Magnetic field strength in microteslas (body frame).</summary>
    public Vector3 MagneticField { get; }

    /// <summary>Time of this reading.</summary>
    public TimeSpan Timestamp { get; }

    /// <summary>Creates a new magnetometer data snapshot.</summary>
    /// <param name="magneticField">Magnetic field in microteslas.</param>
    /// <param name="timestamp">Time of this reading.</param>
    public MagData(Vector3 magneticField, TimeSpan timestamp)
    {
        MagneticField = magneticField;
        Timestamp = timestamp;
    }

    /// <summary>An empty magnetometer reading with zero values.</summary>
    public static readonly MagData Empty = new MagData(Vector3.Zero, TimeSpan.Zero);

    /// <inheritdoc />
    public bool Equals(MagData other)
    {
        return MagneticField.Equals(other.MagneticField)
            && Timestamp.Equals(other.Timestamp);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MagData other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + MagneticField.GetHashCode();
            hash = hash * 31 + Timestamp.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"MagData[Field={MagneticField}µT, T={Timestamp.TotalMilliseconds:F1}ms]";
}
