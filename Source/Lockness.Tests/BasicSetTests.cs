﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CodeDonkeys.Lockness.Tests
{
    [TestFixture]
    public class BasicSetTests
    {
        [Test]
        [TestCase(typeof(HarrisLinkedList<int>))]
        [TestCase(typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>))]
        [TestCase(typeof(SkipListWithBacklink<int>))]
        public void TestSingleElement(Type type)
        {
            var instance = (ISet<int>)Activator.CreateInstance(type, Comparer<int>.Default);

            CollectionAssert.IsEmpty(instance);

            Assert.IsTrue(instance.Add(42));
            Assert.IsFalse(instance.Add(42));

            Assert.IsTrue(instance.Contains(42));
            CollectionAssert.IsNotEmpty(instance);
            CollectionAssert.AreEqual(new [] {42}, instance);

            Assert.IsTrue(instance.Remove(42));
            Assert.IsFalse(instance.Remove(42));

            CollectionAssert.IsEmpty(instance);
        }

        [Test]
        [TestCase(typeof(HarrisLinkedList<int>))]
        [TestCase(typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>))]
        [TestCase(typeof(SkipListWithBacklink<int>))]
        public void TestOrder(Type type)
        {
            var instance = (ISet<int>)Activator.CreateInstance(type, Comparer<int>.Default);

            foreach (var element in Enumerable.Range(0, 42))
            {
                instance.Add(element);
            }

            CollectionAssert.AreEqual(Enumerable.Range(0, 42), instance);
        }

        [Test]
        [TestCase(typeof(HarrisLinkedList<int>))]
        [TestCase(typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>))]
        [TestCase(typeof(SkipListWithBacklink<int>))]
        public void TestDescendingOrder(Type type)
        {
            var instance = (ISet<int>)Activator.CreateInstance(type, Comparer<int>.Default);

            foreach (var element in Enumerable.Range(0, 42).Reverse())
            {
                instance.Add(element);
            }

            CollectionAssert.AreEqual(Enumerable.Range(0, 42), instance);
        }

        [Test]
        [TestCase(typeof(HarrisLinkedList<int>))]
        [TestCase(typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>))]
        [TestCase(typeof(SkipListWithBacklink<int>))]
        public void TestRandomOrder(Type type)
        {
            var instance = (ISet<int>)Activator.CreateInstance(type, Comparer<int>.Default);
            var random = new Random();

            foreach (var element in Enumerable.Range(0, 42).OrderBy(item => random.Next()))
            {
                instance.Add(element);
            }

            CollectionAssert.AreEqual(Enumerable.Range(0, 42), instance);
        }

        [Test]
        [TestCase(typeof(HarrisLinkedList<int>))]
        [TestCase(typeof(HarrisLinkedListWithBacklinkAndSuccessorFlag<int>))]
        [TestCase(typeof(SkipListWithBacklink<int>))]
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
    }
}