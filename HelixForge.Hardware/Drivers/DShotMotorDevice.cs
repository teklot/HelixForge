using System;
using HelixForge.Hardware.Transports;

namespace HelixForge.Hardware.Drivers;

/// <summary>
/// Motor ESC driver that transmits DShot throttle frames through an
/// <see cref="IDShotTransport"/>. Frame timing is delegated to the transport,
/// which is expected to run on a timing-capable co-processor.
/// </summary>
public sealed class DShotMotorDevice : IMotorDevice
{
    private const ushort DisarmedFrame = 0;

    private readonly IDShotTransport _transport;
    private readonly bool _telemetryRequested;
    private double _throttle;

    /// <inheritdoc />
    public string DeviceId { get; }

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public bool IsActive { get; private set; }

    /// <inheritdoc />
    public double Throttle => _throttle;

    /// <summary>Creates a new DShot motor device.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="transport">Frame transport. The driver takes ownership of it.</param>
    /// <param name="telemetryRequested">True to request telemetry on every frame.</param>
    public DShotMotorDevice(string deviceId, IDShotTransport transport, bool telemetryRequested = false)
    {
        DeviceId = deviceId;
        _transport = transport;
        _telemetryRequested = telemetryRequested;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        _transport.SendFrame(DisarmedFrame);
        _throttle = 0.0;
        IsInitialized = true;
        IsActive = true;
    }

    /// <inheritdoc />
    public void SetThrottle(double normalizedThrottle)
    {
        if (!IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is not initialized.");
        if (normalizedThrottle < 0.0 || normalizedThrottle > 1.0)
            throw new ArgumentOutOfRangeException(nameof(normalizedThrottle),
                $"Throttle must be in [0.0, 1.0], got {normalizedThrottle}.");

        _throttle = normalizedThrottle;
        _transport.SendFrame(DShotCodec.Encode(normalizedThrottle, _telemetryRequested));
    }

    /// <inheritdoc />
    public void Reset()
    {
        SetThrottle(0);
        IsActive = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (IsInitialized)
            _transport.SendFrame(DisarmedFrame);

        _transport.Dispose();
        _throttle = 0.0;
        IsInitialized = false;
        IsActive = false;
    }
}
