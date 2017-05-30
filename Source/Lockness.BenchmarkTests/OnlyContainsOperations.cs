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
        private Dictionary<string, Type> setNames = new Dictionary<string, Type>
        {
            {"HarrisLinkedList", typeof(HarrisLinkedList<int>)},
            {"HarrisLinkedListWithBacklinkAndSuccessorFlag", typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>)},
            {"SkipListWithBacklink", typeof(SkipListWithBacklink<int>)},
            {"StrippedHashTable", typeof(StripedHashTable<int>)},
            {"LockBasedHashTable", typeof(LockBasedHashTable<int>)},
            {"LockBasedList", typeof(LockBasedList<int>)}
        };

        private ISet<int> set;

        [Params(100)]//, 10000, 1000000)]
        public int KeysCount { get; set; }

        [Params(1, 2)]//, 8)]
        public int ThreadsCount { get; set; }

        [Params("HarrisLinkedList", "SkipListWithBacklink")]//, "HarrisLinkedListWithBacklinkAndSuccessorFlag", "StrippedHashTable")]
        public string TypeName { get; set; }

        private void ContainsElement(int start, ISet<int> collection)
        {
            for (var i = start; i < KeysCount; i += ThreadsCount)
            {
                collection.Contains(i);
            }
        }

        [Setup]
        public void Initialize()
        {
            set = (ISet<int>)Activator.CreateInstance(setNames[TypeName], Comparer<int>.Default);
            foreach (var i in Enumerable.Range(0, KeysCount))
            {
                set.Add(i);
            }
        }

        [Benchmark]
        public List<Task> ContainsList()
        {
            var actionsList = Enumerable.Range(0, ThreadsCount).Select(i => new Action(() => ContainsElement(i, set))).ToArray();

            var tasks = new List<Task>(ThreadsCount);
            foreach (var action in actionsList)
            {
                tasks.Add(Task.Run(action));
            }

            Task.WaitAll(tasks.ToArray());

            return tasks;
        }
    }
}