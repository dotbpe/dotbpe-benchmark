using DotBPE.Benchmark.Core;
using System;

namespace DotBPE.Benchmark.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = new string[] { "--port", "6201" };
            }

            QpsServerWorker.Run(args);
        }
    }
}
