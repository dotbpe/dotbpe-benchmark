using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

namespace DotBPE.Benchmark.Core
{
    public class ThreadedBenchmark
    {
        List<ThreadStart> runners;

        public ThreadedBenchmark(IEnumerable<ThreadStart> runners)
        {
            this.runners = new List<ThreadStart>(runners);
        }

        public ThreadedBenchmark(int threadCount, Action threadBody)
        {
            this.runners = new List<ThreadStart>();
            for (int i = 0; i < threadCount; i++)
            {
                this.runners.Add(new ThreadStart(() => threadBody()));
            }
        }

        public void Run()
        {
            Console.WriteLine("Running threads.");
            var gcStats = new GCStats();
            var threads = new List<Thread>();
            for (int i = 0; i < runners.Count; i++)
            {
                var thread = new Thread(runners[i]);
                thread.Start();
                threads.Add(thread);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }


            Console.WriteLine("All threads finished (GC Stats Delta: " + gcStats.GetSnapshot() + ")");
        }
    }
}


