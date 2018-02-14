using DotBPE.Benchmark.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotBPE.Benchmark.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            //设置最大的线程数
            ThreadPool.SetMinThreads(200, 100);
            ThreadPool.SetMaxThreads(1000, 200);

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException; ;

            TestClient.Run(args);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                Console.WriteLine(e.Exception.ToString());
            }
            catch
            {

            }
        }
    }
}
