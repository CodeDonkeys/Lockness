using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }

        private void Print()
        {
            var current = list.Head.NextReference.State.Next;
            var str = new StringBuilder();
            while (current.NextReference != null)
            {
                if (!current.NextReference.State.IsMarked)
                    str.AppendFormat($"{current.Value,3}");
                current = current.NextReference.State.Next;
            }
            Console.WriteLine(str.ToString());
        }

        [TestCase(34)]
        [TestCase(300)]
        [TestCase(346)]
        [TestCase(76)]
        public void Insert(int seed)
        {
            var random = new Random(seed);
            var array = new int[30];
            for (int i = 0; i < 30; i++)
            {
                array[i] = random.Next(100);
            }

            for (var i = 0; i < 30; i++)
                list.TryInsert(array[i], array[i]);
            Print();
        }

        [TestCase(34)]
        [TestCase(300)]
        [TestCase(346)]
        [TestCase(76)]
        public void InsertMulti(int seed)
        {
            var random = new Random(seed);

            var array = new int[30];
            for (int i = 0; i < 30; i++)
            {
                array[i] = random.Next(100);
            }
            var str = new StringBuilder();
            foreach (var i in array.Distinct().OrderBy(x => x))
            {
                str.AppendFormat($"{i,3}");
            }
            Console.WriteLine(str.ToString());

            var tasks = new Task[3];
            for (var j = 0; j < 3; j++)
            {
                var k = j;
                tasks[j] = Task.Run(() =>
                {
                    for (var i = 0; i < 10; i++)
                        list.TryInsert(array[i*3 + k], array[i*3 + k]);
                });
            }
            Task.WaitAll(tasks);
            Print();
        }

        [TestCase(34)]
        [TestCase(300)]
        [TestCase(346)]
        [TestCase(76)]
        public void TestDeleteMulti(int seed)
        {
            var random = new Random(seed);

            var array = new int[30];
            for (int i = 0; i < 30; i++)
            {
                array[i] = random.Next(100);
            }

            var deleteArray = new int[10];
            for (int i = 0; i < 10; i++)
            {
                deleteArray[i] = array[random.Next(29)];
            }

            var tasks = new Task[4];
            for (var j = 0; j < 3; j++)
            {
                var k = j;
                tasks[j] = Task.Run(() =>
                {
                    for (var i = 0; i < 10; i++)
                        Insert(array[i * 3 + k], array[i * 3 + k]);
                });
            }

            tasks[3] = Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                    Delete(deleteArray[i]);
            });
            Task.WaitAll(tasks);
            Print();
        }

        [TestCase(34)]
        [TestCase(300)]
        [TestCase(346)]
        [TestCase(76)]
        public void TestDelete(int seed)
        {
            var random = new Random(seed);

            var array = new int[30];
            for (int i = 0; i < 30; i++)
            {
                var elem = random.Next(100);
                list.TryInsert(elem, elem);
                array[i] = elem;
            }

            var str = new StringBuilder();
            foreach (var i in array.Distinct().OrderBy(x => x))
            {
                str.AppendFormat($"{i,3}");
            }
            Console.WriteLine(str.ToString());

            var deleteArray = new int[10];
            for (int i = 0; i < 10; i++)
            {
                var elem = array[random.Next(29)];
                list.TryDelete(elem);
                deleteArray[i] = elem;
            }

            str.Clear();
            foreach (var i in deleteArray.Distinct().OrderBy(x => x))
            {
                str.AppendFormat($"{i,3}");
            }
            Console.WriteLine(str.ToString());

            Print();
        }

        private void Insert(int key, int value)
        {
            var status = list.TryInsert(key, value) ? "done" : "fail";
            Console.WriteLine($"insert {key}:{value} {status}");
        }
        private void Delete(int key)
        {
            var status = list.TryDelete(key) ? "done" : "fail"; ;
            Console.WriteLine($"delete {key} {status}");
        }
    }
}