using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace CodeDonkeys.Lockness.Tests
{
    public class PrintedSkipList : SkipList<int, int>
    {
        public void Print()
        {
            var list = new List<SkipListNode<int, int>>();

            var currentNode = firstLane.Node;
            while (currentNode != null)
            {
                list.Add(currentNode);
                currentNode = currentNode.Next;
            }

            var currentLane = firstLane;
            while (currentLane != null)
            {
                PrintLane(currentLane, list);
                currentLane = currentLane.Down;
            }
            var str = new StringBuilder();
            foreach (var node in list)
            {
                str.AppendFormat($"{node.Value,3}");
            }
            Console.WriteLine(str.ToString());
        }

        private void PrintLane(SkipListLane<int, int> firstLane, List<SkipListNode<int, int>> nodeList)
        {
            var currentLane = firstLane;
            var str = new StringBuilder();
            foreach (var node in nodeList)
            {
                if (node == currentLane.Node)
                {
                    str.Append($"{node.Value,3}");
                    currentLane = currentLane.Next;
                    if (currentLane == null)
                        break;
                }
                else
                {
                    str.Append($"{" ",3}");
                }
            }
            Console.WriteLine(str.ToString());
        }
    }

    [TestFixture]
    public class Tests
    {
        [Test]
        public void aa()
        {
            var random = new Random(500);
            var array = new int[50];
            for (int i = 0; i < 50; i++)
                array[i] = random.Next(100);
            var skiplist = new PrintedSkipList();
            for (int i = 0; i < 50; i++)
                skiplist.Add(array[i], array[i]);
//            foreach (var i in Enumerable.Range(0,10))
//            {
//                skiplist.Add(10 - i, 10 - i);
//            }
            skiplist.Print();
            Console.WriteLine(skiplist.Search(70));
            skiplist.Delete(70);
            skiplist.Print();
            Console.WriteLine(skiplist.Search(70));
            skiplist.Delete(4);
            skiplist.Print();
            skiplist.Delete(99);
            skiplist.Print();
        }
    }
}