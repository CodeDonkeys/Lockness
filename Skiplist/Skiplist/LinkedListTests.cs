using System;
using System.Text;
using NUnit.Framework;

namespace Skiplist
{
    [TestFixture]
    public class LinkedListTests
    {
        private LinkedList<int, int> list;
        [SetUp]
        public void SetUp()
        {
            list = new LinkedList<int, int>();
            var random = new Random(300);
            for (int i = 0; i < 10; i++)
            {
                var k = random.Next(100);
                list.TryInsert(k, k);
            }
            Print();
        }

        private void Print()
        {
            var current = list.Head;
            var str = new StringBuilder();
            while (current.Next != null)
            {
                str.AppendFormat($"{current.Value:3}");
                current = current.Next;
            }
            Console.WriteLine(str.ToString());
        }

        [Test]
        public void aaa()
        {
            
        }
    }
}