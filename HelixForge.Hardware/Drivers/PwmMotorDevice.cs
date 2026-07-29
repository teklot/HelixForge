using System;
using System.Device.Pwm;
using System.Threading;

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
    private readonly int _channel;
    private PwmChannel? _pwmChannel;
    private double _throttle;

    /// <inheritdoc />
    public string DeviceId { get; }

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public bool IsActive { get; private set; }

    /// <inheritdoc />
    public double Throttle => _throttle;

    /// <summary>Creates a new PWM motor device.</summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="chip">PWM chip number (0 on most boards).</param>
    /// <param name="channel">PWM channel number.</param>
    public PwmMotorDevice(string deviceId, int chip, int channel)
    {
        DeviceId = deviceId;
        _chip = chip;
        _channel = channel;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");

        _pwmChannel = PwmChannel.Create(_chip, _channel, Frequency, 0);
        _pwmChannel.Start();

        Thread.Sleep(ArmDelayMs);

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
        _pwmChannel!.DutyCycle = dutyCycle;
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
        _pwmChannel?.Stop();
        _pwmChannel?.Dispose();
        _pwmChannel = null;
        IsInitialized = false;
        IsActive = false;
    }
}
