using System;

namespace HelixForge;

/// <summary>
/// Snapshot of battery state at a specific point in time.
/// </summary>
public readonly struct BatteryData : IEquatable<BatteryData>
{
    /// <summary>Terminal voltage in volts, including sag under load.</summary>
    public double Voltage { get; }

    /// <summary>Current draw in amperes.</summary>
    public double Current { get; }

    /// <summary>State of charge as a fraction in [0.0, 1.0].</summary>
    public double ChargeFraction { get; }

    /// <summary>Time of this reading.</summary>
    public TimeSpan Timestamp { get; }

    /// <summary>Creates a new battery data snapshot.</summary>
    /// <param name="voltage">Terminal voltage in volts.</param>
    /// <param name="current">Current draw in amperes.</param>
    /// <param name="chargeFraction">State of charge as a fraction in [0.0, 1.0].</param>
    /// <param name="timestamp">Time of this reading.</param>
    public BatteryData(double voltage, double current, double chargeFraction, TimeSpan timestamp)
    {
        Voltage = voltage;
        Current = current;
        ChargeFraction = chargeFraction;
        Timestamp = timestamp;
    }

    /// <summary>An empty battery reading with zero values.</summary>
    public static readonly BatteryData Empty = new BatteryData(0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public bool Equals(BatteryData other)
    {
        return Voltage.Equals(other.Voltage)
            && Current.Equals(other.Current)
            && ChargeFraction.Equals(other.ChargeFraction)
            && Timestamp.Equals(other.Timestamp);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BatteryData other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Voltage.GetHashCode();
            hash = hash * 31 + Current.GetHashCode();
            hash = hash * 31 + ChargeFraction.GetHashCode();
            hash = hash * 31 + Timestamp.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"BatteryData[Voltage={Voltage:F2}V, Current={Current:F2}A, SoC={ChargeFraction:P0}, T={Timestamp.TotalMilliseconds:F1}ms]";
}
