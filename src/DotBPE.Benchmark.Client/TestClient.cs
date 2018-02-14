using CommandLine;
using DotBPE.Benchmark.Core;
using DotBPE.Rpc;
using Google.Protobuf.Reflection;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotBPE.Benchmark.Client
{
    public class TestClient
    {
        private static Stopwatch _stopwatch = new Stopwatch();
        private readonly ClientOption _options;

        private TestClient(ClientOption options)
        {
            this._options = options;
        }

        public static void Run(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<ClientOption>(args)
            .WithNotParsed(errors =>
            {
                Console.WriteLine(errors);
                System.Environment.Exit(1);
            })
            .WithParsed(options =>
            {
                var client = new TestClient(options);
                Console.WriteLine("Start to Run....");
                client.Run();
            });
        }

      

        private void Run()
        {
            var statusCodes = new ConcurrentBag<int>();

            var then = DateTime.Now;
            ConsoleWriteLine(ConsoleColor.DarkCyan, "Starting at {0}", then);

            _stopwatch.Restart();

            ConsoleWriteLine(ConsoleColor.Yellow, "[Press C to stop the test]");

            int total = 0;
            var stop = new ConsoleKeyInfo();
            Console.ForegroundColor = ConsoleColor.Cyan;
            var source = new CancellationTokenSource(TimeSpan.FromDays(7));
            var timeTakens = new ConcurrentBag<double>();


            try
            {
                //处理按键C
                Task.Run(() =>
                {
                    while (true)
                    {
                        stop = Console.ReadKey(true);
                        if (stop.KeyChar == 'c')
                            break;
                    }

                    ConsoleWriteLine(ConsoleColor.Red, "...");
                    ConsoleWriteLine(ConsoleColor.Green, "Exiting.... please wait! (it might throw a few more requests)");
                    ConsoleWriteLine(ConsoleColor.Red, "");
                    source.Cancel();

                }, source.Token); // NOT MEANT TO BE AWAITED!!!!

                //开始运行
                ThreadPoolRun(this._options, source, statusCodes, timeTakens, total);

                total = timeTakens.Count;

                Console.WriteLine();
                _stopwatch.Stop();

                ConsoleWriteLine(ConsoleColor.Magenta, "---------------Finished!----------------");
                var now = DateTime.Now;
                ConsoleWriteLine(ConsoleColor.DarkCyan, "Finished at {0} (took {1})", now, now - then);

              
                Thread.Sleep(1000);
                source.Cancel();


                double[] orderedList = (from x in timeTakens
                                        orderby x
                                        select x).ToArray<double>();

                // ----- adding stats of statuses returned
                var stats = statusCodes.GroupBy(x => x)
                           .Select(y => new { Status = y.Key, Count = y.Count() }).OrderByDescending(z => z.Count);

                foreach (var stat in stats)
                {
                    int statusCode = stat.Status;
                    if (statusCode != 0)
                    {
                        ConsoleWriteLine(ConsoleColor.Red, string.Format("Status {0}:    {1}", statusCode, stat.Count));
                    }
                    else
                    {
                        ConsoleWriteLine(ConsoleColor.Green, string.Format("Status {0}:    {1}", statusCode, stat.Count));
                    }

                }

                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Yellow;
                if (!timeTakens.IsEmpty)
                {
                    Console.Write("TPS: " + Math.Round(total * 1000f / _stopwatch.ElapsedMilliseconds, 1));
                    Console.WriteLine(" (requests/second)");
                    Console.WriteLine("Max: " + (timeTakens.Max() * 1000 / Stopwatch.Frequency) + "ms");
                    Console.WriteLine("Min: " + (timeTakens.Min() * 1000 / Stopwatch.Frequency) + "ms");
                    Console.WriteLine("Avg: " + (timeTakens.Average() * 1000 / Stopwatch.Frequency) + "ms");
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine();
                    Console.WriteLine("  50%\tbelow " + Math.Round((double)((orderedList.Percentile<double>(50M) * 1000.0) / ((double)Stopwatch.Frequency))) + "ms");
                    Console.WriteLine("  60%\tbelow " + Math.Round((double)((orderedList.Percentile<double>(60M) * 1000.0) / ((double)Stopwatch.Frequency))) + "ms");
                    Console.WriteLine("  70%\tbelow " + Math.Round((double)((orderedList.Percentile<double>(70M) * 1000.0) / ((double)Stopwatch.Frequency))) + "ms");
                    Console.WriteLine("  80%\tbelow " + Math.Round((double)((orderedList.Percentile<double>(80M) * 1000.0) / ((double)Stopwatch.Frequency))) + "ms");
                    Console.WriteLine("  90%\tbelow " + Math.Round((double)((orderedList.Percentile<double>(90M) * 1000.0) / ((double)Stopwatch.Frequency))) + "ms");
                    Console.WriteLine("  95%\tbelow " + Math.Round((double)((orderedList.Percentile<double>(95M) * 1000.0) / ((double)Stopwatch.Frequency))) + "ms");
                    Console.WriteLine("  98%\tbelow " + Math.Round((double)((orderedList.Percentile<double>(98M) * 1000.0) / ((double)Stopwatch.Frequency))) + "ms");
                    Console.WriteLine("  99%\tbelow " + Math.Round((double)((orderedList.Percentile<double>(99M) * 1000.0) / ((double)Stopwatch.Frequency))) + "ms");
                    Console.WriteLine("99.9%\tbelow " + Math.Round((double)((orderedList.Percentile<double>(99.9M) * 1000.0) / ((double)Stopwatch.Frequency))) + "ms");
                }

                Thread.Sleep(500);

            }
            catch (Exception exception)
            {

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exception);
            }
            

            Console.ResetColor();
        }

        private void ThreadPoolRun(ClientOption clientOption, CancellationTokenSource source, ConcurrentBag<int> statusCodes, ConcurrentBag<double> timeTakens, int total)
        {
            var warmUpTotal = 0;
            var msg = PrepareBenchmarkMessage();
            var workfactory = new WorkItemFactory(clientOption, msg);
            var customThreadPool = new CustomThreadPool(workfactory,
                source,
                clientOption.Concurrency,
                clientOption.WarmupSeconds);

            customThreadPool.WarmupFinished += CustomThreadPool_WarmupFinished; ;

            customThreadPool.WorkItemFinished += (sender, args) =>
            {
                if (args.Result.NoWork)
                    return;
                if (args.Result.IsWarmUp)
                {
                    Interlocked.Increment(ref warmUpTotal);

                    ConsoleWrite(ConsoleColor.Green, "\rWarmup [Users {1}]: {0}", warmUpTotal, customThreadPool.WorkerCount);

                    return;
                }

                statusCodes.Add(args.Result.Status);
                timeTakens.Add(args.Result.Ticks);
                Interlocked.Increment(ref total);
            };

            //执行一次
            workfactory.GetWorkItem(true).Wait() ;

            customThreadPool.Start(clientOption.RunCount);

            while (!source.IsCancellationRequested)
            {
                Thread.Sleep(200);
            }
        }

        private static void CustomThreadPool_WarmupFinished(object sender, EventArgs e)
        {
            Console.WriteLine("-------Warmup Finished--------");
            _stopwatch.Restart();
        }



        private BenchmarkMessage PrepareBenchmarkMessage()
        {
            string v = "拟把疏狂图一醉，对酒当歌，强乐还无味";
            BenchmarkMessage message = new BenchmarkMessage();

            foreach (var field in BenchmarkMessage.Descriptor.Fields.InDeclarationOrder())
            {
                if (field.IsRepeated || field.IsMap)
                {
                    continue;
                }

                switch (field.FieldType)
                {
                    case FieldType.Bool:
                        field.Accessor.SetValue(message, true);
                        break;

                    case FieldType.String:
                        field.Accessor.SetValue(message, v);
                        break;

                    case FieldType.Double:
                        field.Accessor.SetValue(message, 10d);
                        break;

                    case FieldType.SInt32:
                    case FieldType.Int32:
                    case FieldType.SFixed32:
                    case FieldType.Enum:
                    case FieldType.Fixed32:
                    case FieldType.UInt32:
                        field.Accessor.SetValue(message, 99);
                        break;

                    case FieldType.Fixed64:
                    case FieldType.UInt64:
                    case FieldType.SFixed64:
                    case FieldType.Int64:
                    case FieldType.SInt64:
                        field.Accessor.SetValue(message, 99L);
                        break;

                    case FieldType.Float:
                        field.Accessor.SetValue(message, 99f);
                        break;

                    default:
                        break;
                }
            }
            return message;
        }

        internal static void ConsoleWrite(ConsoleColor color, string value, params object[] args)
        {
            var foregroundColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(value, args);
            Console.ForegroundColor = foregroundColor;
        }

        internal static void ConsoleWriteLine(ConsoleColor color, string value, params object[] args)
        {
            var foregroundColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(value, args);
            Console.ForegroundColor = foregroundColor;
        }
    }
}
