using System;

namespace HelixForge;

/// <summary>
/// Immutable 3D vector used for orientation, velocity, and acceleration data.
/// </summary>
public readonly struct Vector3 : IEquatable<Vector3>
{
    /// <summary>X component.</summary>
    public double X { get; }

    /// <summary>Y component.</summary>
    public double Y { get; }

    /// <summary>Z component.</summary>
    public double Z { get; }

    /// <summary>Creates a new 3D vector.</summary>
    /// <param name="x">X component.</param>
    /// <param name="y">Y component.</param>
    /// <param name="z">Z component.</param>
    public Vector3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>A zero vector (0, 0, 0).</summary>
    public static readonly Vector3 Zero = new Vector3(0, 0, 0);

    /// <summary>Returns the magnitude (length) of this vector.</summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>Returns a normalized (unit length) copy of this vector.</summary>
    public Vector3 Normalized
    {
        get
        {
            double mag = Magnitude;
            if (mag < 1e-10)
                return Zero;
            return new Vector3(X / mag, Y / mag, Z / mag);
        }
    }

    /// <summary>Returns the squared magnitude (avoids square root for comparisons).</summary>
    public double MagnitudeSquared => X * X + Y * Y + Z * Z;

    /// <summary>Computes the dot product of two vectors.</summary>
    public static double Dot(Vector3 a, Vector3 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    /// <summary>Computes the cross product of two vectors.</summary>
    public static Vector3 Cross(Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
    }

    /// <summary>Linearly interpolates between two vectors.</summary>
    public static Vector3 Lerp(Vector3 a, Vector3 b, double t)
    {
        return new Vector3(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t);
    }

    /// <summary>Adds two vectors component-wise.</summary>
    public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    /// <summary>Subtracts two vectors component-wise.</summary>
    public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    /// <summary>Negates all components of the vector.</summary>
    public static Vector3 operator -(Vector3 a) => new Vector3(-a.X, -a.Y, -a.Z);
    /// <summary>Scales the vector by a scalar value.</summary>
    public static Vector3 operator *(Vector3 v, double scalar) => new Vector3(v.X * scalar, v.Y * scalar, v.Z * scalar);
    /// <summary>Scales the vector by a scalar value.</summary>
    public static Vector3 operator *(double scalar, Vector3 v) => new Vector3(v.X * scalar, v.Y * scalar, v.Z * scalar);
    /// <summary>Divides the vector by a scalar value.</summary>
    public static Vector3 operator /(Vector3 v, double scalar) => new Vector3(v.X / scalar, v.Y / scalar, v.Z / scalar);

    /// <summary>Returns whether two vectors are equal.</summary>
    public static bool operator ==(Vector3 a, Vector3 b) => a.Equals(b);
    /// <summary>Returns whether two vectors are not equal.</summary>
    public static bool operator !=(Vector3 a, Vector3 b) => !a.Equals(b);

    /// <inheritdoc />
    public bool Equals(Vector3 other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + X.GetHashCode();
            hash = hash * 31 + Y.GetHashCode();
            hash = hash * 31 + Z.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"({X:F4}, {Y:F4}, {Z:F4})";
}
