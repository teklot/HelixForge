namespace HelixForge;

/// <summary>
/// Describes the quality of a GPS position fix.
/// </summary>
public enum GpsFixStatus
{
    /// <summary>No usable position fix available.</summary>
    NoFix = 0,

    /// <summary>Two-dimensional fix (latitude and longitude only).</summary>
    Fix2D = 1,

    /// <summary>Three-dimensional fix (latitude, longitude, and altitude).</summary>
    Fix3D = 2,
}
