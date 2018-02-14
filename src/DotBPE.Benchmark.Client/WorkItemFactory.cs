using DotBPE.Benchmark.Core;
using DotBPE.Protocol.Amp;
using DotBPE.Rpc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotBPE.Benchmark.Client
{
    public class WorkItemFactory : IWorkItemFactory
    {

        private readonly ClientOption _option;
        private ConcurrentQueue<int> _indices;
        private readonly BenchmarkTestClient _client;
        private readonly BenchmarkMessage _reqMsg;
   
     

        public WorkItemFactory(ClientOption option,BenchmarkMessage message)
        {
            _option = option;        
            _indices = new ConcurrentQueue<int>(Enumerable.Range(0, _option.RunCount));
            var proxy = AmpClient.Create(option.Server, option.MultiplexCount);
            _client = new BenchmarkTestClient(proxy);
            _reqMsg = message;
        }

        public async Task<WorkResult> GetWorkItem(bool isWarmup)
        {
            RpcResult<BenchmarkMessage> result;
            var stopwatch = Stopwatch.StartNew();
            if (isWarmup)
            {
                result = await _client.TestAsync(_reqMsg);
                stopwatch.Stop();
                return new WorkResult()
                {
                    Status = (result.Code == 0 && result.Data.Field1 == "OK") ? 0 : -1,
                    Index = 0,
                    Ticks = stopwatch.ElapsedTicks,
                    IsWarmUp = isWarmup
                };
            }
          
            int i = 0;
            var tryDequeue = _indices.TryDequeue(out i);
            if (!tryDequeue)
                return new WorkResult()
                {
                    NoWork = true
                };
            

            if(_option.TestCase == "10ms")
            {
                result = await _client.Test10MSAsync(_reqMsg);
            }
            else if (_option.TestCase == "30ms")
            {
                result = await _client.Test30MSAsync(_reqMsg);
            }
            else
            {
                result = await _client.TestAsync(_reqMsg);

            }
            stopwatch.Stop();


            return new WorkResult()
            {
                Status = (result.Code == 0 && result.Data.Field1 == "OK") ? 0 : -1,
                Index = i,
                Ticks = stopwatch.ElapsedTicks,
                IsWarmUp = isWarmup
            };
        }
    }
}
