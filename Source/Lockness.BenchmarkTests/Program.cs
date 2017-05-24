using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace CodeDonkeys.Lockness.BenchmarkTests
{
    [RankColumn]
    [Config(typeof(AddConfig1))]
    public class AddWrapper
    {
        private ISet<int> harrisList;
        private ISet<int> harrisListWithBacklinks;
        private ISet<int> skiplist;
        private ISet<int> hashTable;
        private int element;

        [Setup]
        public void Initialize()
        {
            harrisList = new HarrisLinkedList<int>(Comparer<int>.Default);
            harrisListWithBacklinks = new HarrisLinkedListWithBacklinkAndSuccessorFlag<int>(Comparer<int>.Default);
            skiplist = new SkipListWithBacklink<int>(Comparer<int>.Default);
            hashTable = new StripedHashTable<int>(Comparer<int>.Default);

            element = -1;
        }
    
        [Benchmark]
        public bool AddHarrisLinkedList()
        {
            element++;
            return harrisList.Add(element);
        }

        [Benchmark]
        public bool AddHarrisListWithBacklinks()
        {
            element++;
            return harrisListWithBacklinks.Add(element);
        }

        [Benchmark]
        public bool AddSkiplist()
        {
            element++;
            return skiplist.Add(element);
        }

        [Benchmark]
        public bool AddHashTable()
        {
            element++;
            return hashTable.Add(element);
        }
    }

    internal class AddConfig : ManualConfig
    {
        public AddConfig()
        {
            Add(new Job("OneProcessJob", RunMode.Short, new EnvMode
            {
                Gc = { Server = true, Concurrent = true },
                Jit = Jit.RyuJit,
                Platform = Platform.X64,
                Runtime = Runtime.Clr,
                Affinity = new IntPtr(0x0001)
            }));
            Add(new Job("TwoProcessJob", RunMode.Short, new EnvMode
            {
                Gc = { Server = true, Concurrent = true },
                Jit = Jit.RyuJit,
                Platform = Platform.X64,
                Runtime = Runtime.Clr,
                Affinity = new IntPtr(0x0003)
            }));
            Add(new Job("ManyProcessJob", RunMode.Short, new EnvMode
            {
                Gc = { Server = true, Concurrent = true },
                Jit = Jit.RyuJit,
                Platform = Platform.X64,
                Runtime = Runtime.Clr,
                Affinity = new IntPtr(0x007F)
            }));
            Add(StatisticColumn.Max);
            Add(StatisticColumn.OperationsPerSecond);
            Add(RPlotExporter.Default, CsvExporter.Default);
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<AddWrapper>();
        }
    }
}
