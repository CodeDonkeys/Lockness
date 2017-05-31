using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Exporters;

namespace CodeDonkeys.Lockness.BenchmarkTests
{
    [RankColumn]
    [Config(typeof(GeneralConfig))]
    [PlainExporter]
    public class OnlyContainsOperations
    {
        private readonly Dictionary<string, Type> setNames = new Dictionary<string, Type>
        {
            {"HarrisLinkedList", typeof(HarrisLinkedList<int>)},
            {"HarrisLinkedListWithBacklinkAndSuccessorFlag", typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>)},
            {"SkipListWithBacklink", typeof(SkipListWithBacklink<int>)},
            {"StrippedHashTable", typeof(StripedHashTable<int>)},
            {"LockBasedHashTable", typeof(LockBasedHashTable<int>)},
            {"LockBasedList", typeof(LockBasedList<int>)}
        };

        private Task[] tasks;

        private ISet<int> set;

        [Params(100, 10000)]
        public int KeysCount { get; set; }

        [Params(1, 2, 8)]
        public int ThreadsCount { get; set; }

        [Params("HarrisLinkedList", "HarrisLinkedListWithBacklinkAndSuccessorFlag", "SkipListWithBacklink", "StrippedHashTable", "LockBasedHashTable", "LockBasedList")]
        public string TypeName { get; set; }

        private void ContainsElement(int start, ISet<int> collection)
        {
            for (var i = start; i < KeysCount; i += ThreadsCount)
            {
                collection.Contains(i);
            }
        }

        [IterationSetup]
        public void Initialize()
        {
            foreach (var i in Enumerable.Range(0, KeysCount))
            {
                set.Add(i);
            }
            tasks = Enumerable.Range(0, ThreadsCount).Select(i => new Task(() => ContainsElement(i, set))).ToArray();
        }

        [GlobalSetup]
        public void SetInitialize()
        {
            set = (ISet<int>)Activator.CreateInstance(setNames[TypeName], Comparer<int>.Default);
        }

        [Benchmark]
        public Task[] ContainsList()
        {
            foreach (var task in tasks)
            {
                task.Start();
            }

            Task.WaitAll(tasks);

            return tasks;
        }
    }
}