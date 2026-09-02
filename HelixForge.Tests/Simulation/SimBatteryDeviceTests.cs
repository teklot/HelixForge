using HelixForge;
using HelixForge.Simulation;

namespace HelixForge.Tests.Simulation;

public class SimBatteryDeviceTests
{
    [Fact]
    public void Initialize_SetsIsInitialized()
    {
        var battery = new SimBatteryDevice("batt-01", new BatterySimConfig());
        Assert.False(battery.IsInitialized);

        battery.Initialize();

        Assert.True(battery.IsInitialized);
    }

    [Fact]
    public void Initialize_FullyCharged_ReportsFullVoltage()
    {
        var battery = new SimBatteryDevice("batt-01", new BatterySimConfig { FullVoltage = 12.6, EmptyVoltage = 9.0 });
        battery.Initialize();

        var data = battery.Read();
        Assert.Equal(1.0, data.ChargeFraction, 6);
        Assert.Equal(12.6, data.Voltage, 4);
    }

    [Fact]
    public void Update_WithLoad_DrainsCapacityAndSagsVoltage()
    {
        var config = new BatterySimConfig
        {
            FullVoltage = 12.6,
            EmptyVoltage = 9.0,
            InternalResistance = 0.1,
            CapacityAh = 1.0,
            InitialCharge = 1.0
        };
        var battery = new SimBatteryDevice("batt-01", config, new[] { new ConstantConsumer(10.0) });
        battery.Initialize();

        // 10A * 0.1 ohm = 1.0V sag
        Assert.Equal(11.6, battery.Read().Voltage, 4);

        // 10A for 3600s (1h) drains 1.0Ah of a 1.0Ah pack -> empty.
        for (int i = 0; i < 3600; i++)
            battery.Update(TimeSpan.FromSeconds(1));

        Assert.Equal(0.0, battery.Read().ChargeFraction, 6);
        // Terminal voltage = OCV(9.0) - sag(10A * 0.1ohm = 1.0) = 8.0.
        Assert.Equal(8.0, battery.Read().Voltage, 4);
    }

    [Fact]
    public void Update_NoLoad_KeepsCharge()
    {
        var battery = new SimBatteryDevice("batt-01", new BatterySimConfig(), Array.Empty<ICurrentConsumer>());
        battery.Initialize();

        battery.Update(TimeSpan.FromSeconds(60));

        Assert.Equal(1.0, battery.Read().ChargeFraction, 6);
    }

    [Fact]
    public void Update_DeadbandMotor_DrawsNoCurrent()
    {
        var config = new MotorSimConfig { MaxCurrentAmps = 15.0 };
        var motor = new SimMotorDevice("motor-01", config);
        motor.Initialize();
        motor.SetThrottle(0.0);

        var battery = new SimBatteryDevice("batt-01",
            new BatterySimConfig { CapacityAh = 1.0 },
            new ICurrentConsumer[] { motor });
        battery.Initialize();

        // Throttle below deadband yields zero current draw.
        Assert.Equal(0.0, battery.Read().Current, 6);
    }

    private sealed class ConstantConsumer : ICurrentConsumer
    {
        private readonly double _current;
        public ConstantConsumer(double current) => _current = current;
        public double Current => _current;
    }
}
