using System;
using System.Threading;
using HelixForge.Hardware.Transports;

namespace HelixForge.Hardware.Drivers;

/// <summary>
/// Real motor ESC driver using standard 50Hz PWM (1000–2000µs pulse width).
/// Compatible with most hobby ESCs supporting conventional PWM input.
/// </summary>
public sealed class PwmMotorDevice : IMotorDevice
{
    private const int Frequency = 50;
    private const double MinPulseMs = 1.0;
    private const double MaxPulseMs = 2.0;
    private const int ArmDelayMs = 2000;

    private readonly int _chip;
    private readonly int _channelNumber;
    private readonly TimeSpan _armDelay;
    private IPwmChannel? _channel;
    private double _throttle;

    /// <inheritdoc />
    public string DeviceId { get; }

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public bool IsActive { get; private set; }

    /// <inheritdoc />
    public double Throttle => _throttle;

    /// <summary>Creates a new PWM motor device on a physical PWM channel.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="chip">PWM chip number (0 on most boards).</param>
    /// <param name="channel">PWM channel number.</param>
    public PwmMotorDevice(string deviceId, int chip, int channel)
    {
        DeviceId = deviceId;
        _chip = chip;
        _channelNumber = channel;
        _armDelay = TimeSpan.FromMilliseconds(ArmDelayMs);
    }

    /// <summary>Creates a new PWM motor device on a supplied PWM channel.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="channel">PWM channel. The driver takes ownership of it.</param>
    /// <param name="armDelay">How long to hold the disarmed pulse on initialization before accepting commands. Defaults to 2 seconds.</param>
    public PwmMotorDevice(string deviceId, IPwmChannel channel, TimeSpan? armDelay = null)
    {
        DeviceId = deviceId;
        _channel = channel;
        _armDelay = armDelay ?? TimeSpan.FromMilliseconds(ArmDelayMs);
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        IPwmChannel channel = _channel ?? (_channel = new PwmChannelAdapter(_chip, _channelNumber, Frequency));
        channel.Start();

        if (_armDelay > TimeSpan.Zero)
            Thread.Sleep(_armDelay);

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
        double dutyCycle = (MinPulseMs + normalizedThrottle * (MaxPulseMs - MinPulseMs)) / (1000.0 / Frequency);
        _channel!.DutyCycle = dutyCycle;
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
        _channel?.Dispose();
        _channel = null;
        IsInitialized = false;
        IsActive = false;
    }
}
