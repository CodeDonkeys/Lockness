using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeDonkeys.Lockness.BenchmarkTests
{
    internal enum Operation
    {
        Contains,
        Add,
        Remove
    }

    internal class OperationsGenerator
    {
        private HashSet<int> internalSet;
        private ISet<int> collection;
        private Random rand = new Random();

        public Action[] GenerateOperations(int containsCount, int addCount, int removeCount, ISet<int> collection)
        {
            internalSet = new HashSet<int>();
            foreach (var element in collection)
            {
                internalSet.Add(element);
            }
            this.collection = collection;

            return Enumerable.Range(0, containsCount)
                .Select(x => Operation.Contains)
                .Union(Enumerable.Range(0, addCount).Select(x => Operation.Add))
                .Union(Enumerable.Range(0, removeCount).Select(x => Operation.Remove))
                .OrderBy(x => Guid.NewGuid())
                .Select(CreateTask)
                .ToArray();
        }

        private Action CreateTask(Operation operation)
        {
            switch (operation)
            {
                case Operation.Contains:
                {
                    var elementIndex = rand.Next(internalSet.Count);
                    var element = internalSet.ElementAt(elementIndex);
                    return () => collection.Contains(element);
                }
                case Operation.Add:
                {
                    var newElement = rand.Next(Int32.MaxValue);
                    internalSet.Add(newElement);
                    return () => collection.Add(newElement);
                }
                case Operation.Remove:
                {
                    var elementIndex = rand.Next(internalSet.Count);
                    var element = internalSet.ElementAt(elementIndex);
                    internalSet.Remove(element);
                    return () => collection.Remove(element);
                }
                default:
                {
                    return () => { };
                }
            }
        }
    }
}