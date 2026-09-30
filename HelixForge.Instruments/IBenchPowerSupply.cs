using System;

namespace HelixForge.Instruments;

/// <summary>
/// Contract for a bench power supply with settable output voltage/current
/// limits and steady-state readback.
/// </summary>
public interface IBenchPowerSupply : IDisposable
{
    /// <summary>Unique identifier for the device.</summary>
    string DeviceId { get; }

    /// <summary>True once the device has been initialized.</summary>
    bool IsInitialized { get; }

    /// <summary>Gets the configured output enable state.</summary>
    bool OutputEnabled { get; }

    /// <summary>Turns the output on or off.</summary>
    void SetOutputEnabled(bool enabled);

    /// <summary>Sets the target output voltage in volts.</summary>
    void SetVoltage(double volts);

    /// <summary>Gets the target output voltage in volts.</summary>
    double TargetVoltage { get; }

    /// <summary>Gets the current output voltage in volts.</summary>
    double OutputVoltage { get; }

    /// <summary>Gets the measured output current in amperes.</summary>
    double OutputCurrent { get; }

    /// <summary>Gets the maximum settable voltage in volts.</summary>
    double MaxVoltage { get; }

    /// <summary>Gets the maximum settable current in amperes.</summary>
    double MaxCurrent { get; }

    /// <summary>Advances the supply state by one simulation step.</summary>
    void UpdateClock();
}