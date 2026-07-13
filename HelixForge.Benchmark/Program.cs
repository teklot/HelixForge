using BenchmarkDotNet.Running;

namespace HelixForge.Benchmark;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<SimulationEngineBenchmark>();
    }
}
