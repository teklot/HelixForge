using System;

namespace HelixForge.Simulation;

/// <summary>
/// Configuration for the simulation engine.
/// </summary>
public sealed class SimulationConfig
{
    /// <summary>Time step for each simulation iteration. Default is 10ms (100Hz).</summary>
    public TimeSpan TimeStep { get; set; } = TimeSpan.FromMilliseconds(10);

    /// <summary>Maximum number of steps per <c>Run</c> call. 0 = unlimited.</summary>
    public int MaxIterations { get; set; }

    /// <summary>Initial simulation time. Default is zero.</summary>
    public TimeSpan StartTime { get; set; } = TimeSpan.Zero;

    /// <summary>Random seed for deterministic simulation. Null uses system clock.</summary>
    public int? RandomSeed { get; set; }
}
