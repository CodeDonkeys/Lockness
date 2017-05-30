using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace CodeDonkeys.Lockness.BenchmarkTests
{
    internal class GeneralConfig : ManualConfig
    {
        public GeneralConfig()
        {
            Add(new Job("GeneralJob", RunMode.Default, new EnvMode
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
            Add(MemoryDiagnoser.Default);
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
