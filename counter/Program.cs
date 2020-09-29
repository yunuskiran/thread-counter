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
            var path = @"D:\gits\thread-counter\counter\Files\Sample.txt";
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
            var content = File.ReadAllText(path);
            var sentences = Regex.Split(content, @"(?<=[\.!\?])\s+").ToList();
            var average = Math.Round(sentences.Average(_ => _.Split(' ').Length));
            List<List<WaitHandle>> waitHandles = new List<List<WaitHandle>>();
            WaitHandle[] waitHandless = new WaitHandle[threadCount];
            int counter = 0;
            while (sentences.Skip((threadCount - 1) * counter).Take(threadCount).Any())
            {
                var data = sentences.Skip((threadCount - 1) * counter).Take(threadCount).ToList();
                for (int i = 0; i < data.Count; i++)
                {
                    var j = i;
                    var handle = new EventWaitHandle(false, EventResetMode.ManualReset);
                    var thread = new Thread(() =>
                    {
                        Process(data[j]);
                        handle.Set();
                    });
                    thread.Start();
                    waitHandless[j] = handle;
                }

                waitHandles.Add(waitHandless.ToList());
                counter++;
            }

            foreach (var item in waitHandles)
            {
                if (item != null)
                    WaitHandle.WaitAll(item.Where(_ => _ != null).ToArray());
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
