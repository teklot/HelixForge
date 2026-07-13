using HelixForge;

namespace HelixForge.Tests.Core;

public class Vector3Tests
{
    [Fact]
    public void Constructor_SetsComponents()
    {
        var v = new Vector3(1.0, 2.0, 3.0);
        Assert.Equal(1.0, v.X);
        Assert.Equal(2.0, v.Y);
        Assert.Equal(3.0, v.Z);
    }

    [Fact]
    public void Zero_ReturnsZeroVector()
    {
        var v = Vector3.Zero;
        Assert.Equal(0.0, v.X);
        Assert.Equal(0.0, v.Y);
        Assert.Equal(0.0, v.Z);
    }

    [Fact]
    public void Magnitude_CalculatesCorrectly()
    {
        var v = new Vector3(3.0, 4.0, 0.0);
        Assert.Equal(5.0, v.Magnitude, 4);
    }

    [Fact]
    public void MagnitudeSquared_AvoidsSqrt()
    {
        var v = new Vector3(3.0, 4.0, 0.0);
        Assert.Equal(25.0, v.MagnitudeSquared, 4);
    }

    [Fact]
    public void Normalized_ReturnsUnitVector()
    {
        var v = new Vector3(3.0, 4.0, 0.0);
        var n = v.Normalized;
        Assert.Equal(0.6, n.X, 4);
        Assert.Equal(0.8, n.Y, 4);
        Assert.Equal(0.0, n.Z, 4);
        Assert.Equal(1.0, n.Magnitude, 4);
    }

    [Fact]
    public void Normalized_ZeroVector_ReturnsZero()
    {
        var v = Vector3.Zero;
        var n = v.Normalized;
        Assert.Equal(Vector3.Zero, n);
    }

    [Fact]
    public void OperatorAdd_AddsCorrectly()
    {
        var a = new Vector3(1.0, 2.0, 3.0);
        var b = new Vector3(4.0, 5.0, 6.0);
        var result = a + b;
        Assert.Equal(new Vector3(5.0, 7.0, 9.0), result);
    }

    [Fact]
    public void OperatorSubtract_SubtractsCorrectly()
    {
        var a = new Vector3(5.0, 7.0, 9.0);
        var b = new Vector3(1.0, 2.0, 3.0);
        var result = a - b;
        Assert.Equal(new Vector3(4.0, 5.0, 6.0), result);
    }

    [Fact]
    public void OperatorMultiply_ScalesCorrectly()
    {
        var v = new Vector3(1.0, 2.0, 3.0);
        var result = v * 2.0;
        Assert.Equal(new Vector3(2.0, 4.0, 6.0), result);
    }

    [Fact]
    public void DotProduct_CalculatesCorrectly()
    {
        var a = new Vector3(1.0, 2.0, 3.0);
        var b = new Vector3(4.0, 5.0, 6.0);
        Assert.Equal(32.0, Vector3.Dot(a, b), 4);
    }

    [Fact]
    public void CrossProduct_CalculatesCorrectly()
    {
        var a = new Vector3(1.0, 0.0, 0.0);
        var b = new Vector3(0.0, 1.0, 0.0);
        var result = Vector3.Cross(a, b);
        Assert.Equal(new Vector3(0.0, 0.0, 1.0), result);
    }

    [Fact]
    public void Lerp_InterpolatesCorrectly()
    {
        var a = new Vector3(0.0, 0.0, 0.0);
        var b = new Vector3(10.0, 10.0, 10.0);
        var result = Vector3.Lerp(a, b, 0.5);
        Assert.Equal(new Vector3(5.0, 5.0, 5.0), result);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new Vector3(1.0, 2.0, 3.0);
        var b = new Vector3(1.0, 2.0, 3.0);
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Vector3(1.0, 2.0, 3.0);
        var b = new Vector3(1.0, 2.0, 4.0);
        Assert.NotEqual(a, b);
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var v = new Vector3(1.5, 2.5, 3.5);
        Assert.Equal("(1.5000, 2.5000, 3.5000)", v.ToString());
    }
}
