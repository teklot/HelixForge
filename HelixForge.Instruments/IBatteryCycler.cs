using System;

namespace HelixForge.Instruments;

/// <summary>
/// Contract for a battery cycler: applies charge/discharge current profiles
/// to a cell simulator and reports voltage/current/charge state.
/// </summary>
public interface IBatteryCycler : IDisposable
{
    /// <summary>Unique identifier for the device.</summary>
    string DeviceId { get; }

    /// <summary>True once the device has been initialized.</summary>
    bool IsInitialized { get; }

    /// <summary>Gets the applied present-current set point in amperes (positive = charge, negative = discharge).</summary>
    double SetPointCurrent { get; }

    /// <summary>Sets the applied current in amperes (positive = charge, negative = discharge).</summary>
    void SetCurrent(double amperes);

    /// <summary>Gets the cell voltage in volts.</summary>
    double CellVoltage { get; }

    /// <summary>Gets the charge fraction in the range 0..1.</summary>
    double ChargeFraction { get; }

    /// <summary>Advances the cycler (and cell model) by one simulation step.</summary>
    void UpdateClock();
}