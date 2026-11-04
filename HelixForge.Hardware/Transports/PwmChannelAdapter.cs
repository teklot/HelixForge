using System;
using System.Device.Pwm;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// <see cref="IPwmChannel"/> implementation backed by <see cref="PwmChannel"/>.
/// </summary>
public sealed class PwmChannelAdapter : IPwmChannel
{
    private readonly PwmChannel _channel;

    /// <summary>Creates a PWM channel on the given chip and channel numbers.</summary>
    /// <param name="chip">PWM chip number (0 on most boards).</param>
    /// <param name="channel">PWM channel number.</param>
    /// <param name="frequencyHz">PWM frequency in hertz.</param>
    /// <param name="dutyCycle">Initial duty cycle in the range [0.0, 1.0].</param>
    public PwmChannelAdapter(int chip, int channel, int frequencyHz, double dutyCycle = 0.0)
    {
        _channel = PwmChannel.Create(chip, channel, frequencyHz, dutyCycle);
    }

    /// <inheritdoc />
    public int Frequency
    {
        get => _channel.Frequency;
        set => _channel.Frequency = value;
    }

    /// <inheritdoc />
    public double DutyCycle
    {
        get => _channel.DutyCycle;
        set => _channel.DutyCycle = value;
    }

    /// <inheritdoc />
    public void Start() => _channel.Start();

    /// <inheritdoc />
    public void Stop() => _channel.Stop();

    /// <inheritdoc />
    public void Dispose() => _channel.Dispose();
}
