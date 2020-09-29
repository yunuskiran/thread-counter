using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace counter
{
    class Program
    {
        private static ReaderWriterLockSlim lockSlim;
        private static Dictionary<string, int> innerCache;
        static void Main(string[] args)
        {
            var defaultThreadCount = 5;
            var path = @"C:\Users\TCYKIRAN\source\repos\counter\counter\Files\Sample.txt";
            Console.Write("Thread Count: ");
            int.TryParse(Console.ReadLine(), out var threadCount);
            if (threadCount == 0)
                threadCount = defaultThreadCount;
            Console.Write("Path: ");
            var tempPath = Console.ReadLine();
            if (File.Exists(tempPath))
                path = tempPath;

            Do(threadCount, path);
            Console.ReadKey();
        }

        private static void Do(int threadCount, string path)
        {
            lockSlim = new ReaderWriterLockSlim();
            innerCache = new Dictionary<string, int>();
            var content = File.ReadAllText(@"C:\Users\TCYKIRAN\source\repos\counter\counter\Files\Sample.txt");
            var sentences = Regex.Split(content, @"(?<=[\.!\?])\s+").ToList();
            var average = Math.Round(sentences.Average(_ => _.Split(' ').Length));
            List<Thread> threads = new List<Thread>();
            ThreadPool.SetMaxThreads(threadCount, 1);
            foreach (var item in sentences)
            {
                ThreadPool.QueueUserWorkItem(x => Process(item));
            }

            Console.WriteLine($"Sentence Count: {sentences.Count}");
            Console.WriteLine($"Avg. Word Count: {average}");
            foreach (var item in innerCache.OrderByDescending(_ => _.Value))
            {
                Console.WriteLine($"{item.Key} {item.Value}");
            }
        }

        static void Process(string sentence)
        {
            foreach (var words in sentence.Split(' '))
            {
                AddOrIncrement(words.Replace("?", "").Replace(".", "").Replace("!", ""));
            }
        }

        public static void AddOrIncrement(string key)
        {
            lockSlim.EnterUpgradeableReadLock();
            try
            {
                if (innerCache.TryGetValue(key, out var result))
                {
                    innerCache[key] = result + 1;
                }
                else
                {
                    lockSlim.EnterWriteLock();
                    try
                    {
                        innerCache.Add(key, 1);
                    }
                    finally
                    {
                        lockSlim.ExitWriteLock();
                    }
                }
            }
            finally
            {
                lockSlim.ExitUpgradeableReadLock();
            }
        }
    }
}
