using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotBPE.Benchmark.Client
{
    public class ClientOption
    {
        [Option('t', "testcase", Default = "0ms", HelpText = "对应的测试用例")]
        public string TestCase { get; set; }

        [Option('s', "server", Default = "127.0.0.1:6201", HelpText = "服务器地址")]
        public string Server { get; set; }

        [Option('c', "concurrency", Default = 1, HelpText = "线程数")]
        public int Concurrency { get; set; }


        [Option('m', "multiplex", Default = 5, HelpText = "共享链接数")]
        public int MultiplexCount { get; set; }

        [Option('r', "runcount", Default = 10000, HelpText = "执行总次数,将平分到每个并发client中")]
        public int RunCount { get; set; }


        [Option('W', "WarmupSeconds", Required = false, Default = 0, HelpText = "Number of seconds to gradually increase number of concurrent users. Warm-up calls do not affect stats.")]
        public int WarmupSeconds { get; set; }
    }
}
