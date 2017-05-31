using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

namespace CodeDonkeys.Lockness.BenchmarkTests
{
    internal class GeneralConfig : ManualConfig
    {
        public GeneralConfig()
        {
            Add(new Job("GeneralJob", new RunMode{RunStrategy = RunStrategy.Monitoring}, new EnvMode
            {
                Gc = { Server = true, Concurrent = true },
                Jit = Jit.RyuJit,
                Platform = Platform.X64,
                Runtime = Runtime.Clr
            }));
            Add(StatisticColumn.Max);
            Add(StatisticColumn.OperationsPerSecond);
            Add(CsvMeasurementsExporter.Default);
            Add(RPlotExporter.Default);
        }
    }
    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<OnlyContainsOperations>();
        }
    }
}
