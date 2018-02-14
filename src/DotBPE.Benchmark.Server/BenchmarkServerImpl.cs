using DotBPE.Benchmark.Core;
using DotBPE.Rpc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotBPE.Benchmark.Server
{
    public class BenchmarkServerImpl : BenchmarkTestBase
    {
       
        public override async Task<RpcResult<BenchmarkMessage>> Test10MSAsync(BenchmarkMessage req)
        {
            req.Field1 = "OK";
            req.Field2 = 100;

            var res = new RpcResult<BenchmarkMessage>();
            res.Data = req;

            await Task.Delay(10);
           
            return res;
        }

        public override async Task<RpcResult<BenchmarkMessage>> Test30MSAsync(BenchmarkMessage req)
        {
            req.Field1 = "OK";
            req.Field2 = 100;

            var res = new RpcResult<BenchmarkMessage>();
            res.Data = req;

            await Task.Delay(30);
            return res;
        }

        public override Task<RpcResult<BenchmarkMessage>> TestAsync(BenchmarkMessage req)
        {
            req.Field1 = "OK";
            req.Field2 = 100;

            var res = new RpcResult<BenchmarkMessage>();
            res.Data = req;

            return Task.FromResult(res);
        }
    }
}
