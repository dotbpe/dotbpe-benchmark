using System;
using System.Collections.Generic;
using System.Text;

namespace DotBPE.Benchmark.Client
{
    public class WorkResult
    {
        public int Status { get; set; }

        public int Index { get; set; }

        public double Ticks { get; set; }

        public bool NoWork { get; set; }

        public bool IsWarmUp  { get; set; }
    }
}
