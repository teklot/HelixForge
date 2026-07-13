using System;

namespace HelixForge.Simulation;

/// <summary>
/// Event arguments for simulation step completion.
/// </summary>
public sealed class SimulationStepCompletedEventArgs : EventArgs
{
    /// <summary>Current simulation time after this step.</summary>
    public TimeSpan CurrentTime { get; }

    /// <summary>Step number (0-based).</summary>
    public int StepNumber { get; }

    /// <summary>The time step that was executed.</summary>
    public TimeSpan TimeStep { get; }

    /// <summary>Creates new step completed event args.</summary>
    public SimulationStepCompletedEventArgs(TimeSpan currentTime, int stepNumber, TimeSpan timeStep)
    {
        CurrentTime = currentTime;
        StepNumber = stepNumber;
        TimeStep = timeStep;
    }
}
