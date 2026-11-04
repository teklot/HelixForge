using System;

namespace HelixForge.Hardware.Transports;

/// <summary>
/// Minimal PWM output channel abstraction used by motor drivers. Implemented for
/// real hardware by <see cref="PwmChannelAdapter"/> and by fakes in unit tests.
/// </summary>
public interface IPwmChannel : IDisposable
{
    /// <summary>Gets or sets the PWM frequency in hertz.</summary>
    int Frequency { get; set; }

    /// <summary>Gets or sets the duty cycle in the range [0.0, 1.0].</summary>
    double DutyCycle { get; set; }

    /// <summary>Starts generating PWM output.</summary>
    void Start();

    /// <summary>Stops generating PWM output.</summary>
    void Stop();
}
