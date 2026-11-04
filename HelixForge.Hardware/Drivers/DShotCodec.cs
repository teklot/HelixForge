using System;

namespace HelixForge.Hardware.Drivers;

/// <summary>
/// Encodes and validates 16-bit DShot throttle frames: 11 throttle bits, one
/// telemetry-request bit, and a 4-bit CRC computed over the upper 12 bits.
/// </summary>
public static class DShotCodec
{
    /// <summary>Lowest throttle value that is interpreted as a throttle command (values 1-47 are commands).</summary>
    public const int MinThrottleValue = 48;

    /// <summary>Highest throttle value in a DShot frame.</summary>
    public const int MaxThrottleValue = 2047;

    private const int FrameCrcBits = 4;

    /// <summary>Encodes a normalized throttle into a complete 16-bit DShot frame.</summary>
    /// <param name="normalizedThrottle">Throttle in the range [0.0, 1.0]; 0.0 encodes the disarmed frame.</param>
    /// <param name="telemetryRequest">True to request a telemetry response from the ESC.</param>
    /// <returns>The encoded frame.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the throttle is outside [0.0, 1.0].</exception>
    public static ushort Encode(double normalizedThrottle, bool telemetryRequest = false)
    {
        if (!(normalizedThrottle >= 0.0 && normalizedThrottle <= 1.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(normalizedThrottle),
                normalizedThrottle,
                "Throttle must be in [0.0, 1.0].");
        }

        int throttleValue = normalizedThrottle > 0.0
            ? MinThrottleValue + (int)Math.Round(normalizedThrottle * (MaxThrottleValue - MinThrottleValue), MidpointRounding.AwayFromZero)
            : 0;

        int value = (throttleValue << 1) | (telemetryRequest ? 1 : 0);
        int crc = ComputeCrc(value);

        return (ushort)((value << FrameCrcBits) | crc);
    }

    /// <summary>Decodes a frame, validating its CRC.</summary>
    /// <param name="frame">The 16-bit frame to decode.</param>
    /// <param name="throttleValue">The decoded throttle value (0 when disarmed).</param>
    /// <param name="telemetryRequest">True when the frame requests telemetry.</param>
    /// <returns>True when the frame CRC is valid; otherwise false.</returns>
    public static bool TryDecode(ushort frame, out int throttleValue, out bool telemetryRequest)
    {
        int value = frame >> FrameCrcBits;
        int crc = frame & 0x0F;

        if (crc != ComputeCrc(value))
        {
            throttleValue = 0;
            telemetryRequest = false;
            return false;
        }

        throttleValue = value >> 1;
        telemetryRequest = (value & 1) != 0;
        return true;
    }

    /// <summary>Returns whether the frame's CRC is valid.</summary>
    /// <param name="frame">The 16-bit frame to validate.</param>
    /// <returns>True when the CRC matches; otherwise false.</returns>
    public static bool IsValid(ushort frame)
    {
        return (frame & 0x0F) == ComputeCrc(frame >> FrameCrcBits);
    }

    /// <summary>Computes the 4-bit CRC over the upper 12 bits of a DShot frame.</summary>
    /// <param name="value">The 12-bit payload (throttle and telemetry bits).</param>
    /// <returns>The 4-bit CRC.</returns>
    public static int ComputeCrc(int value)
    {
        return (value ^ (value >> 4) ^ (value >> 8)) & 0x0F;
    }
}
