using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Histogram;
using App.Metrics.Reporting;
using CommandLine;
using DotBPE.Benchmark.Core;
using DotBPE.Protocol.Amp;
using DotBPE.Rpc;
using Google.Protobuf.Reflection;

namespace DotBPE.Benchmark.Client
{
    public class TestClient
    {

        private class ClientOption
        {
            [Option("testcase", Default = "0ms", HelpText = "对应的测试用例")]
            public string TestCase { get; set; }

            [Option("server", Default = "127.0.0.1:6201", HelpText = "服务器地址")]
            public string Server { get; set; }

            [Option("tc", Default = 1 , HelpText = "线程数")]
            public int ThreadCount { get; set; }


            [Option("mc", Default = 5, HelpText = "共享链接数")]
            public int MultiplexCount { get; set; }

            [Option("rc", Default = 10000, HelpText = "执行次数")]
            public int RunCount { get; set; }
        }

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
        private BenchmarkTestClient _client;
        private IMetricsRoot _metrics;
        private void Run()
        {
            var proxy = AmpClient.Create(this._options.Server, this._options.MultiplexCount);

            _client = new BenchmarkTestClient(proxy);

            _metrics = new MetricsBuilder()
                    .Report.ToConsole()
                   .Build();

            Console.WriteLine("------------------开始预热-----------------");
            Prepare(_client).Wait();
            Console.WriteLine("------------------预热结束，开始执行-----------------");

            ThreadRun(this._options.TestCase);

            //关闭连接
            proxy.CloseAsync().Wait();
            Console.WriteLine("press any key to quit!");
            Console.ReadKey();

        }

        private void ThreadRun(string testcase)
        {

            var msg = PrepareBenchmarkMessage();
            var rc = this._options.RunCount;


            var total_run = rc * this._options.ThreadCount;

            var elapsedTime = new HistogramOptions
            {
                Name = "Elapsed Time",
                MeasurementUnit = Unit.Bytes
            };

            var receive_total = 0;
            var receive_ok = 0;

            var thbench = new ThreadedBenchmark(this._options.ThreadCount, () =>
            {
                for (int i = 0; i < rc; i++)
                {
                    var spw = Stopwatch.StartNew();

                    RpcResult<BenchmarkMessage> res;
                    if(testcase == "10ms")
                    {
                        res = RunBenchmark10MS(msg).Result;
                    }
                    else if (testcase == "30ms")
                    {
                        res = RunBenchmark30MS(msg).Result;
                    }
                    else
                    {
                        res = RunBenchmark0MS(msg).Result;
                    }
                    spw.Stop();
                    _metrics.Measure.Histogram.Update(elapsedTime, spw.ElapsedMilliseconds);
                    Interlocked.Increment(ref receive_total);
                    if (res.Code == 0 && res.Data.Field1 == "OK")
                    {
                        Interlocked.Increment(ref receive_ok);
                    }

                }
            });

            Stopwatch sw = new Stopwatch();

            sw.Start();

            thbench.Run();

            sw.Stop();

            Console.WriteLine("--------------------- total --------------------------------");
            Console.WriteLine("sent     requests    : {0}", total_run);
            Console.WriteLine("received requests    : {0}", receive_total);
            Console.WriteLine("received requests_ok : {0}", receive_ok);
            Console.WriteLine("ElapsedMilliseconds  : {0}", sw.ElapsedMilliseconds);
            Console.WriteLine("ops per second       : {0}", (int)((double) total_run * 1000 / sw.ElapsedMilliseconds));

            var tasks = _metrics.ReportRunner.RunAllAsync();

            Task.WhenAll(tasks).Wait() ;
        }

        private Task<RpcResult<BenchmarkMessage>> RunBenchmark10MS(BenchmarkMessage reqMsg)
        {          
            return _client.Test10MSAsync(reqMsg,60000);
        }

        private Task<RpcResult<BenchmarkMessage>> RunBenchmark30MS(BenchmarkMessage reqMsg)
        {
           
            return _client.Test30MSAsync(reqMsg, 60000);
        }

        private Task<RpcResult<BenchmarkMessage>> RunBenchmark0MS(BenchmarkMessage reqMsg)
        {            
            return _client.TestAsync(reqMsg, 60000);
        }

       

        private Task Prepare(BenchmarkTestClient client)
        {
            var message = PrepareBenchmarkMessage();
            for (var i = 0; i < 100; i++)
            {
                client.TestAsync(message).Wait();
            }
            return Task.CompletedTask;
        }

        private BenchmarkMessage PrepareBenchmarkMessage()
        {
            string v = "拟把疏狂图一醉，对酒当歌，强乐还无味";
            BenchmarkMessage message = new BenchmarkMessage();
          
            foreach (var field in BenchmarkMessage.Descriptor.Fields.InDeclarationOrder())
            {
                if(field.IsRepeated || field.IsMap)
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
    }
}
