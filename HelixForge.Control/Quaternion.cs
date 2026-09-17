using System;

namespace HelixForge.Control;

/// <summary>
/// Immutable quaternion used for attitude representation and manipulation.
/// Convention: (w, x, y, z) with w the scalar part.
/// </summary>
public readonly struct Quaternion : IEquatable<Quaternion>
{
    /// <summary>Scalar (w) component.</summary>
    public double W { get; }

    /// <summary>X component.</summary>
    public double X { get; }

    /// <summary>Y component.</summary>
    public double Y { get; }

    /// <summary>Z component.</summary>
    public double Z { get; }

    /// <summary>Creates a new quaternion.</summary>
    /// <param name="w">Scalar component.</param>
    /// <param name="x">X component.</param>
    /// <param name="y">Y component.</param>
    /// <param name="z">Z component.</param>
    public Quaternion(double w, double x, double y, double z)
    {
        W = w;
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>The identity quaternion (no rotation).</summary>
    public static readonly Quaternion Identity = new Quaternion(1, 0, 0, 0);

    /// <summary>Returns a unit-length copy of this quaternion.</summary>
    public Quaternion Normalized
    {
        get
        {
            double norm = Math.Sqrt(W * W + X * X + Y * Y + Z * Z);
            if (norm < 1e-12)
                return Identity;
            return new Quaternion(W / norm, X / norm, Y / norm, Z / norm);
        }
    }

    /// <summary>Returns the conjugate (same rotation, opposite axis sign).</summary>
    public Quaternion Conjugate => new Quaternion(W, -X, -Y, -Z);

    /// <summary>
    /// Converts this quaternion to Euler angles (roll, pitch, yaw) in radians,
    /// using the ZYX (yaw-pitch-roll) convention.
    /// </summary>
    /// <returns>Euler angles as (roll, pitch, yaw).</returns>
    public Vector3 ToEuler()
    {
        double w = W, x = X, y = Y, z = Z;

        double roll = Math.Atan2(2.0 * (w * x + y * z), 1.0 - 2.0 * (x * x + y * y));
        double sinp = 2.0 * (w * y - z * x);
        double pitch = Math.Asin(Math.Max(-1.0, Math.Min(1.0, sinp)));
        double yaw = Math.Atan2(2.0 * (w * z + x * y), 1.0 - 2.0 * (y * y + z * z));
        return new Vector3(roll, pitch, yaw);
    }

    /// <summary>
    /// Creates a quaternion from Euler angles (roll, pitch, yaw) in radians,
    /// using the ZYX (yaw-pitch-roll) convention.
    /// </summary>
    /// <param name="euler">Euler angles as (roll, pitch, yaw).</param>
    /// <returns>The corresponding quaternion.</returns>
    public static Quaternion FromEuler(Vector3 euler)
    {
        return FromEuler(euler.X, euler.Y, euler.Z);
    }

    /// <summary>
    /// Creates a quaternion from Euler roll, pitch, and yaw angles in radians.
    /// </summary>
    public static Quaternion FromEuler(double roll, double pitch, double yaw)
    {
        double cy = Math.Cos(yaw * 0.5);
        double sy = Math.Sin(yaw * 0.5);
        double cp = Math.Cos(pitch * 0.5);
        double sp = Math.Sin(pitch * 0.5);
        double cr = Math.Cos(roll * 0.5);
        double sr = Math.Sin(roll * 0.5);

        double w = cr * cp * cy + sr * sp * sy;
        double x = sr * cp * cy - cr * sp * sy;
        double y = cr * sp * cy + sr * cp * sy;
        double z = cr * cp * sy - sr * sp * cy;
        return new Quaternion(w, x, y, z).Normalized;
    }

    /// <summary>Multiplies two quaternions (Hamilton product).</summary>
    public static Quaternion operator *(Quaternion a, Quaternion b)
    {
        return new Quaternion(
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z,
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W);
    }

    /// <summary>
    /// Rotates a vector by this quaternion.
    /// </summary>
    public Vector3 Rotate(Vector3 v)
    {
        Quaternion p = new Quaternion(0, v.X, v.Y, v.Z);
        Quaternion result = this * p * Conjugate;
        return new Vector3(result.X, result.Y, result.Z);
    }

    /// <summary>
    /// Returns the angle (in radians) of the rotation represented by this quaternion.
    /// </summary>
    public double Angle => 2.0 * Math.Acos(Math.Max(-1.0, Math.Min(1.0, W)));

    /// <summary>Returns whether two quaternions are equal.</summary>
    public static bool operator ==(Quaternion a, Quaternion b) => a.Equals(b);
    /// <summary>Returns whether two quaternions are not equal.</summary>
    public static bool operator !=(Quaternion a, Quaternion b) => !a.Equals(b);

    /// <inheritdoc />
    public bool Equals(Quaternion other)
    {
        return W.Equals(other.W) && X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Quaternion other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + W.GetHashCode();
            hash = hash * 31 + X.GetHashCode();
            hash = hash * 31 + Y.GetHashCode();
            hash = hash * 31 + Z.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"({W:F4}, {X:F4}, {Y:F4}, {Z:F4})";
}