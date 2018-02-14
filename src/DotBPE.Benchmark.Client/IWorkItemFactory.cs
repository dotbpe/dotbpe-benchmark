using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotBPE.Benchmark.Client
{
    public interface IWorkItemFactory
    {
        Task<WorkResult> GetWorkItem(bool isWarmup);
    }
}
