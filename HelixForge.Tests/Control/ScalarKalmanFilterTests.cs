using HelixForge.Control;

namespace HelixForge.Tests.Control;

public class ScalarKalmanFilterTests
{
    [Fact]
    public void FirstMeasurement_BecomesEstimate()
    {
        var filter = new ScalarKalmanFilter(new ScalarKalmanConfig
        {
            InitialCovariance = 1e9
        });

        filter.Update(5.0);

        Assert.Equal(5.0, filter.Estimate, 5);
    }

    [Fact]
    public void Observations_ConvergeTowardTruth()
    {
        var filter = new ScalarKalmanFilter(new ScalarKalmanConfig
        {
            ProcessNoise = 1.0,
            MeasurementNoise = 1.0
        });

        filter.Update(9.0);
        filter.Update(9.0);
        filter.Update(9.0);
        filter.Update(9.0);
        filter.Update(9.0);

        // Low-noise measurement of a 9.0 signal overwhelmingly wins over the initial guess.
        Assert.InRange(filter.Estimate, 8.9, 9.1);
    }

    [Fact]
    public void Predict_AddsCovariance()
    {
        var filter = new ScalarKalmanFilter(new ScalarKalmanConfig
        {
            ProcessNoise = 1.0,
            InitialCovariance = 0.0
        });

        filter.Predict(2.0);

        Assert.Equal(2.0, filter.Covariance, 10);
    }

    [Fact]
    public void Update_ReducesCovariance()
    {
        var filter = new ScalarKalmanFilter(new ScalarKalmanConfig
        {
            MeasurementNoise = 4.0
        });

        filter.Update(1.0);
        double before = filter.Covariance;

        for (int i = 0; i < 20; i++)
            filter.Update(1.0);

        Assert.True(filter.Covariance < before);
    }

    [Fact]
    public void Reset_RestoresInitialState()
    {
        var filter = new ScalarKalmanFilter(new ScalarKalmanConfig
        {
            InitialEstimate = 3.0,
            InitialCovariance = 7.0
        });

        filter.Update(9.0);
        filter.Reset();

        Assert.Equal(3.0, filter.Estimate, 10);
        Assert.Equal(7.0, filter.Covariance, 10);
    }

    [Fact]
    public void RepeatedRuns_AreDeterministic()
    {
        double RunOnce()
        {
            var filter = new ScalarKalmanFilter(new ScalarKalmanConfig());
            for (int i = 0; i < 100; i++)
                filter.Update(2.0 + Math.Sin(i * 0.1), dt: 0.01);
            return filter.Estimate;
        }

        Assert.Equal(RunOnce(), RunOnce(), 12);
    }
}