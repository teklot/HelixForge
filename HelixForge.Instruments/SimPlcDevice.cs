using System;
using HelixForge;

namespace HelixForge.Instruments;

/// <summary>
/// A simple deterministic simulated PLC with a configurable AND/OR rung
/// ladder program. On each scan, Q0 = I0 &amp;&amp; I1 and Q1 = I2 || I3.
/// </summary>
public sealed class SimPlcDevice : IPlcDevice
{
    private readonly bool[] _inputs;
    private readonly bool[] _outputs;
    private readonly ITelemetryPublisher? _telemetry;
    private bool _initialized;

    /// <summary>
    /// Creates a new simulated PLC.
    /// </summary>
    /// <param name="deviceId">Unique device identifier.</param>
    /// <param name="inputCount">Number of digital inputs.</param>
    /// <param name="outputCount">Number of digital outputs.</param>
    /// <param name="telemetry">Optional telemetry publisher receiving output states per scan.</param>
    public SimPlcDevice(string deviceId, int inputCount, int outputCount, ITelemetryPublisher? telemetry = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        if (inputCount < 0) throw new ArgumentOutOfRangeException(nameof(inputCount));
        if (outputCount < 0) throw new ArgumentOutOfRangeException(nameof(outputCount));
        _inputs = new bool[inputCount];
        _outputs = new bool[outputCount];
        _telemetry = telemetry;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public int InputCount => _inputs.Length;

    /// <inheritdoc/>
    public int OutputCount => _outputs.Length;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException($"Device '{DeviceId}' is already initialized.");
        _initialized = true;
    }

    /// <inheritdoc/>
    public void SetInput(int index, bool state)
    {
        if (index < 0 || index >= _inputs.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        _inputs[index] = state;
    }

    /// <inheritdoc/>
    public bool ReadInput(int index)
    {
        if (index < 0 || index >= _inputs.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _inputs[index];
    }

    /// <inheritdoc/>
    public bool ReadOutput(int index)
    {
        if (index < 0 || index >= _outputs.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _outputs[index];
    }

    /// <inheritdoc/>
    public void ScanOnce()
    {
        for (int i = 0; i < _outputs.Length; i++)
        {
            _outputs[i] = EvaluateOutput(i);
            _telemetry?.Publish(DeviceId, $"plc.q{i}.state", _outputs[i] ? 1.0 : 0.0, TimeSpan.Zero);
        }
    }

    private bool EvaluateOutput(int outputIndex)
    {
        switch (outputIndex)
        {
            case 0:
                return GetInput(0) && GetInput(1);
            case 1:
                return GetInput(2) || GetInput(3);
            default:
                return false;
        }
    }

    private bool GetInput(int index) => index < _inputs.Length && _inputs[index];

    /// <inheritdoc/>
    public void Dispose() { }
}