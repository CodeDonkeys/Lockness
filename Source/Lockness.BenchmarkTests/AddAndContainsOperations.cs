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
    public class AddAndContainsOperations
    {
        private OperationsGenerator generator = new OperationsGenerator();
        private Task[] tasks;

        private Dictionary<string, Type> setNames = new Dictionary<string, Type>
        {
            {"HarrisLinkedList", typeof(HarrisLinkedList<int>)},
            {"HarrisLinkedListWithBacklinkAndSuccessorFlag", typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>)},
            {"SkipListWithBacklink", typeof(SkipListWithBacklink<int>)},
            {"StrippedHashTable", typeof(StripedHashTable<int>)},
            {"LockBasedHashTable", typeof(LockBasedHashTable<int>)},
            {"LockBasedList", typeof(LockBasedList<int>)}
        };

        [Params(1, 2, 8)]
        public int ThreadsCount { get; set; }

        [Params("HarrisLinkedList", "HarrisLinkedListWithBacklinkAndSuccessorFlag", "SkipListWithBacklink", "StrippedHashTable", "LockBasedHashTable", "LockBasedList")]
        public string TypeName { get; set; }

        [Params(100, 10000, 1000000)]
        public int OperationsCount { get; set; }

        [Setup]
        public void Initialize()
        {
            var set = (ISet<int>)Activator.CreateInstance(setNames[TypeName], Comparer<int>.Default);
            foreach (var i in Enumerable.Range(0, 100))
            {
                set.Add(i);
            }

            var operations = generator.GenerateOperations((int)(OperationsCount * 0.8), (int)(OperationsCount * 0.2), 0, set);
            tasks = Enumerable.Range(0, ThreadsCount).Select(i => new Task(() => InvokeActions(i, operations))).ToArray();
        }

        private void InvokeActions(int start, Action[] operations)
        {
            for (var i = start; i < operations.Length; i += ThreadsCount)
            {
                operations[i].Invoke();
            }
        }
        
        [Benchmark]
        public Task[] AddAndContainsElement()
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