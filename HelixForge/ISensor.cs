using System;

namespace HelixForge;

/// <summary>
/// Represents a sensor device that produces data and advances
/// its internal state via time-step updates.
/// </summary>
public interface ISensor : IDevice
{
    /// <summary>
    /// Advances the sensor's internal state by the specified elapsed time.
    /// Called once per simulation step in simulation mode.
    /// In real hardware mode, this is a no-op or reads latest hardware state.
    /// </summary>
    /// <param name="deltaTime">The elapsed time since the last update.</param>
    void Update(TimeSpan deltaTime);
}
