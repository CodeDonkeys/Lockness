using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CodeDonkeys.Lockness.Tests
{
    [TestFixture]
    public class MultithreadingTests
    {
        [Test]
        [TestCase(typeof(HarrisLinkedList<int>))]
        [TestCase(typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>))]
        [TestCase(typeof(SkipListWithBacklink<int>))]
        [TestCase(typeof(StripedHashTable<int>))]
        public void TestSingleElement(Type type)
        {
            var instance = (ISet<int>)Activator.CreateInstance(type, Comparer<int>.Default);

            CollectionAssert.IsEmpty(instance);

            var task0 = Task.Run(() => Assert.IsTrue(instance.Add(42)));
            var task1 = Task.Run(() => { while (!instance.Remove(42)) { } });

            Task.WaitAll(task0, task1);

            //тут проблема. IsEmpty не работает без еще одного Search, потому что элемент еще реально не удалился
            Assert.IsFalse(instance.Contains(42));

            CollectionAssert.IsEmpty(instance);
        }

        [Test]
        [TestCase(typeof(HarrisLinkedList<int>))]
        [TestCase(typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>))]
        [TestCase(typeof(SkipListWithBacklink<int>))]
        [TestCase(typeof(StripedHashTable<int>))]
        public void TestTwoThreadsInsert(Type type)
        {
            var instance = (ISet<int>)Activator.CreateInstance(type, Comparer<int>.Default);

            var func = new Action<int, ISet<int>>((start, collection) =>
            {
                for (var i = start; i < 42; i += 2)
                {
                    collection.Add(i);
                }

            });

            var task0 = Task.Run(() => func(0, instance));
            var task1 = Task.Run(() => func(1, instance));

            Task.WaitAll(task0, task1);

            CollectionAssert.AreEqual(Enumerable.Range(0, 42), instance);
        }


        [Test]
        [TestCase(typeof(HarrisLinkedList<int>))]
        [TestCase(typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>))]
        [TestCase(typeof(SkipListWithBacklink<int>))]
        [TestCase(typeof(StripedHashTable<int>))]
        public void TestTwoThreadsDelete(Type type)
        {
            var instance = (ISet<int>)Activator.CreateInstance(type, Comparer<int>.Default);

            foreach (var i in Enumerable.Range(0,42))
            {
                instance.Add(i);
            }

            var func = new Action<int, ISet<int>>((start, collection) =>
            {
                for (var i = start; i < 42; i += 2)
                {
                    collection.Remove(i);
                }

            });
            var task0 = Task.Run(() => func(0, instance));
            var task1 = Task.Run(() => func(1, instance));

            Task.WaitAll(task0, task1);

            Assert.IsFalse(instance.Contains(42));

            CollectionAssert.AreEqual(Enumerable.Empty<int>(), instance);
        }
    }
}