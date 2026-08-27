# HelixForge — Hardware Abstraction and Simulation for .NET

[![CI](https://github.com/teklot/HelixForge/actions/workflows/ci.yml/badge.svg)](https://github.com/teklot/HelixForge/actions)
[![NuGet Version](https://img.shields.io/nuget/v/HelixForge)](https://www.nuget.org/packages/HelixForge)
[![.NET](https://img.shields.io/badge/.NET-net10.0%20%7C%20netstandard2.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue)](LICENSE)

Every hardware-focused team eventually builds the same thing: a sensor class, a motor class, a way to read IMU data, a way to command ESCs, some telemetry pipeline. Usually scattered across projects, each with different interfaces, none composable, all tied to specific hardware. Testing requires physical devices.

HelixForge is a shared abstraction layer for hardware on .NET — **the vocabulary that makes real and simulated hardware interchangeable.** Not a driver framework, not a telemetry database, not a control library. A common device interface that sits between your application and your hardware, giving every IMU reading, every motor command, every GPS fix the same shape regardless of source.

**Guiding principle:** Simulation must never depend on telemetry. Telemetry must never affect behavior.

## The Problem

```csharp
// Typical codebase — every project reinvents:
class MyImu { public double[] ReadAccel(); }              // no orientation
class DroneMotor { public void SetPower(double p); }      // no throttle model
struct GpsReading { public double Lat, Lon; }             // no altitude, no velocity
interface ISensor { void Update(); }                       // no time step, no reset
```

No two implementations agree. IMUs return raw arrays in different orders. Motors have no response model. GPS readings mix altitude and velocity inconsistently. Simulation is either a separate executable or tightly coupled to the real driver. Switching from real to simulated hardware requires rewriting control logic.

**HelixForge eliminates the seam.** It provides the shared interfaces that acquisition, simulation, and telemetry all agree on, so your control code runs identically on a desk test bench or on real hardware.

## How It Works

The entire domain model lives in `HelixForge` — **core abstractions with zero simulation or telemetry dependencies.**

```
┌──────────────────────────────────────────────────────────────┐
│                        HelixForge                            │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐  │
│  │    IDevice     │  │    ISensor     │  │   IActuator    │  │
│  │  .DeviceId     │  │  .Update(dt)   │  │  .IsActive     │  │
│  │  .IsInitialized│  │  .Read<T>()    │  │                │  │
│  │  .Initialize() │  │                │  │                │  │
│  │  .Reset()      │  │                │  │                │  │
│  └────────────────┘  └────────────────┘  └────────────────┘  │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐  │
│  │  IImuDevice    │  │  IGpsDevice    │  │  IMotorDevice  │  │
│  │  .Read()       │  │  .Read()       │  │  .SetThrottle()│  │
│  │  ImuData       │  │  GpsData       │  │  .Throttle     │  │
│  └────────────────┘  └────────────────┘  └────────────────┘  │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐  │
│  │IMagnetometerDev│  │IBarometerDevice│  │  IServoDevice  │  │
│  │  .Read()       │  │  .Read()       │  │  .SetAngle()   │  │
│  │  MagData       │  │  BarometerData │  │  .Angle        │  │
│  └────────────────┘  └────────────────┘  └────────────────┘  │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐  │
│  │    Vector3     │  │ DeviceRegistry │  │ITelemetryPub   │  │
│  │  .X, .Y, .Z    │  │  .Register()   │  │  .Publish()    │  │
│  │  +, -, *, Dot  │  │  .GetById()    │  │                │  │
│  │  Cross, Lerp   │  │  .GetByType()  │  │                │  │
│  └────────────────┘  └────────────────┘  └────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │   ExecutionMode: Simulation = 0, Real = 1              │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

Every data type is **`readonly struct`** — zero-allocation on hot paths. `ImuData`, `GpsData`, and `Vector3` are all value types with structural equality, `IEquatable<T>`, and `GetHashCode`.

### Devices? No Magic Strings

Instead of casting, type-checking, or passing string identifiers through your pipeline:

```csharp
// HelixForge uses typed interfaces — compile-time checked, IntelliSense discoverable
var imu = registry.GetByType<IImuDevice>();
var motor = registry.GetByType<IMotorDevice>();
var gps = registry.GetByType<IGpsDevice>();
```

The registry is type-safe. `GetByType<T>()` returns the first registered device of that interface type. No string-based service locator pattern.

### Simulation Without Coupling

The simulation engine is a separate assembly. It implements `ISensor.Update(TimeSpan)` on each simulated device:

```csharp
var imu = new SimImuDevice("imu-01", new ImuSimConfig
{
    AccelerometerNoise = 0.005,
    GyroscopeNoise = 0.002,
    RandomSeed = 42
});
var motor = new SimMotorDevice("motor-01", new MotorSimConfig
{
    MaxRpm = 10000,
    ResponseTimeConstant = 0.05
});

registry.Register(imu);
registry.Register(motor);
```

Your control code never knows whether `IImuDevice` is real or simulated. The interface is the contract.

### Telemetry as a Side-Channel

Telemetry is injected as an `ITelemetryPublisher` — never a dependency:

```csharp
var telemetry = new TelemetryBus();
telemetry.AddSink(new ConsoleSink());
telemetry.AddSink(new DelegateSink(e => logger.Log(e.ToString())));

var engine = new SimulationEngine(registry, telemetry, config);
```

The simulation engine publishes device state each step. Telemetry sinks receive events but never influence simulation behavior. The PRD enforces this: **Simulation → Telemetry is one-way.**

### Deterministic Execution

Seed-based RNG produces identical results across runs:

```csharp
var config = new SimulationConfig
{
    TimeStep = TimeSpan.FromMilliseconds(10),
    RandomSeed = 42
};
```

Same seed, same noise, same orientation trajectory, same motor response. Essential for regression testing control algorithms.

## Use Cases

### Control Algorithm Development

```
SimImuDevice → PID Controller → SimMotorDevice → Observe
```

Develop and tune PID, LQR, or MPC controllers entirely in simulation. The same `IImuDevice` interface runs on real hardware when you're ready to deploy.

### Hardware-in-the-Loop Testing

```
Real IMU → ISensor.Update() → Control Loop → IMotorDevice.SetThrottle()
                         ↓
                    TelemetryBus → ConsoleSink / DelegateSink
```

Swap `SimImuDevice` for a real driver without changing a single line of control code.

### Regression Testing

```
Run 1: seed=42, 5 seconds, assert orientation converges
Run 2: seed=42, 5 seconds, assert identical trajectory
```

Deterministic simulation enables bit-exact regression tests for safety-critical control logic.

### Telemetry Archival

```
SimulationEngine → TelemetryBus → Custom Sink → CSV / SQLite / Network
```

The delegate sink pattern makes it trivial to write telemetry to any backend.

## Technical Differentiators

| vs. | HelixForge |
|---|---|
| **Homemade sensor classes** | Typed interfaces, simulation models, telemetry side-channel, zero-allocation value types |
| **Simulink / Modelica** | .NET-native, no MATLAB license, runs in CI/CD, composes with existing .NET tooling |
| **Robotics Middleware (ROS)** | No external process broker, no message serialization overhead, in-process simulation |
| **Hardware vendor SDKs** | Tied to specific hardware. HelixForge normalizes any source into one interface |
| **Python simulation** | No static typing, no .NET interop. HelixForge brings the same concept to .NET with `readonly struct`, `Span<T>`, compile-time safety |

## Packages

| Package | Description |
|---|---|
| **HelixForge** | Core abstractions: `IDevice`, `ISensor`, `IActuator`, `IImuDevice`, `IGpsDevice`, `IMagnetometerDevice`, `IBarometerDevice`, `IMotorDevice`, `IServoDevice`, `Vector3`, `ImuData`, `GpsData`, `MagData`, `BarometerData`, `GpsFixStatus`, `DeviceRegistry`, `ITelemetryPublisher` |
| **HelixForge.Simulation** | Deterministic simulation engine: `SimulationEngine`, `SimImuDevice`, `SimMotorDevice`, `SimServoDevice`, `SimGpsDevice`, `SimMagDevice`, `SimBarometerDevice`, configurable noise, drift, and dropout models |
| **HelixForge.Telemetry** | Telemetry bus: `TelemetryBus`, `TelemetryEvent`, `ConsoleSink`, `DelegateSink`, `ITelemetrySink` |
| **HelixForge.Hardware** | Real hardware device drivers: BMI160 IMU (I2C), NMEA GPS (UART), PWM motor ESC — more drivers coming in future releases |

## Installation

```shell
dotnet add package HelixForge
dotnet add package HelixForge.Simulation
dotnet add package HelixForge.Telemetry
```

## Quick Start

```csharp
using HelixForge;
using HelixForge.Simulation;
using HelixForge.Telemetry;

// Configure simulation
var config = new SimulationConfig
{
    TimeStep = TimeSpan.FromMilliseconds(10), // 100Hz
    RandomSeed = 42
};

// Register devices
var registry = new DeviceRegistry();
var imu = new SimImuDevice("imu-01", new ImuSimConfig
{
    AccelerometerNoise = 0.005,
    GyroscopeNoise = 0.002,
    InitialOrientation = new Vector3(0.2, 0.1, 0.0)
});
var motor = new SimMotorDevice("motor-01", new MotorSimConfig
{
    MaxRpm = 10000,
    ResponseTimeConstant = 0.05
});

registry.Register(imu);
registry.Register(motor);

// Set up telemetry
var telemetry = new TelemetryBus();
telemetry.AddSink(new ConsoleSink());

// Run simulation with closed-loop control
var engine = new SimulationEngine(registry, telemetry, config);
engine.Run(TimeSpan.FromSeconds(5), time =>
{
    var reading = imu.Read();
    double error = 0.0 - reading.Orientation.X;
    motor.SetThrottle(Math.Clamp(0.5 + error, 0.0, 1.0));
});
```

## Domain Model

### `IDevice`
Base interface for all devices. Provides identity, lifecycle, and reset.

```csharp
public interface IDevice
{
    string DeviceId { get; }
    bool IsInitialized { get; }
    void Initialize();
    void Reset();
}
```

### `ISensor`
Extends `IDevice`. Adds time-stepped data acquisition.

```csharp
public interface ISensor : IDevice
{
    void Update(TimeSpan deltaTime);
}
```

### `IImuDevice`
Specialized sensor: orientation, angular velocity, and acceleration.

```csharp
var imu = registry.GetByType<IImuDevice>();
ImuData data = imu.Read();
Vector3 orientation = data.Orientation;    // radians
Vector3 angVel = data.AngularVelocity;     // rad/s
Vector3 accel = data.Acceleration;         // m/s^2
```

### `IMotorDevice`
Specialized actuator: throttle control.

```csharp
var motor = registry.GetByType<IMotorDevice>();
motor.SetThrottle(0.75);   // 0.0 to 1.0
double t = motor.Throttle;  // current throttle
```

### `IGpsDevice`
Specialized sensor: position, velocity, and fix quality.

```csharp
var gps = registry.GetByType<IGpsDevice>();
GpsData data = gps.Read();
double lat = data.Latitude;          // decimal degrees
double lon = data.Longitude;
double alt = data.Altitude;          // meters
Vector3 vel = data.Velocity;         // m/s
GpsFixStatus fix = data.FixStatus;   // NoFix / Fix2D / Fix3D
```

### `IMagnetometerDevice`
Specialized sensor: magnetic field strength in the body frame.

```csharp
var mag = registry.GetByType<IMagnetometerDevice>();
MagData data = mag.Read();
Vector3 field = data.MagneticField;  // microteslas (µT)
```

### `IBarometerDevice`
Specialized sensor: atmospheric pressure and barometric altitude.

```csharp
var baro = registry.GetByType<IBarometerDevice>();
BarometerData data = baro.Read();
double pressure = data.Pressure;     // hPa
double altitude = data.Altitude;     // meters (barometric)
```

### `IServoDevice`
Specialized actuator: positional angle control with slew-rate dynamics.

```csharp
var servo = registry.GetByType<IServoDevice>();
servo.SetAngle(90.0);   // degrees (clamped to limits)
double a = servo.Angle; // current achieved angle
```

### `Vector3`
Immutable 3D vector with arithmetic, dot product, cross product, lerp, and normalization.

```csharp
var a = new Vector3(1.0, 2.0, 3.0);
var b = new Vector3(4.0, 5.0, 6.0);
double dot = Vector3.Dot(a, b);        // 32.0
var cross = Vector3.Cross(a, b);       // (-3, 6, -3)
var mid = Vector3.Lerp(a, b, 0.5);    // (2.5, 3.5, 4.5)
```

### `DeviceRegistry`
Type-safe container for all registered devices.

```csharp
var registry = new DeviceRegistry();
registry.Register(imu);
IDevice? found = registry.GetById("imu-01");
IImuDevice? byType = registry.GetByType<IImuDevice>();
IEnumerable<IImuDevice> all = registry.GetAllByType<IImuDevice>();
```

### `SimulationEngine`
Deterministic time-step coordinator. Advances virtual time and calls `Update()` on all registered sensors each step.

```csharp
var engine = new SimulationEngine(registry, telemetry, config);
engine.Step();                                    // one step
engine.Run(TimeSpan.FromSeconds(10));             // run for duration
engine.Run(TimeSpan.FromSeconds(10), t => { });  // with callback
engine.Reset();                                   // return to initial state
```

### `TelemetryBus`
Central bus that routes events to registered sinks.

```csharp
var bus = new TelemetryBus();
bus.AddSink(new ConsoleSink());
bus.AddSink(new DelegateSink(e => processEvent(e)));
bus.Publish("imu-01", "orientation.x", 1.5, TimeSpan.FromSeconds(1));
bus.RemoveSink(sink);
bus.Dispose();
```

## Supported Frameworks

- **.NET 10+**: Optimized for maximum performance and Native AOT compatibility.
- **.NET Standard 2.0**: Broad compatibility across legacy .NET platforms.

## Roadmap

### Phase 1 — Core Abstractions ✓
- Device interfaces, value types, device registry, telemetry bus

### Phase 2 — Simulation ✓
- Deterministic simulation engine, IMU/GPS/magnetometer/barometer/motor/servo models, noise and drift, PID demo
- Per-device console samples via `--sample <mag|baro|servo|gps>`

### Phase 3 — Hardware Drivers ✓
- Real IMU driver (BMI160, I2C), GPS driver (NMEA), ESC driver (PWM)
- Future: SPI IMU, DShot ESC, UBX GPS, additional chips

### Phase 4 — Control Library (planned)
- Built-in PID controller, LQR, state estimation, trajectory generation
