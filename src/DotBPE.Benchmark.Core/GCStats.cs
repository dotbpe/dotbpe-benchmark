using System;
using System.Collections.Generic;
using System.Text;

namespace DotBPE.Benchmark.Core
{
    internal class GCStats
    {
        readonly object myLock = new object();
        GCStatsSnapshot lastSnapshot;

        public GCStats()
        {
            lastSnapshot = new GCStatsSnapshot(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
        }

        public GCStatsSnapshot GetSnapshot(bool reset = false)
        {
            lock (myLock)
            {
                var newSnapshot = new GCStatsSnapshot(GC.CollectionCount(0) - lastSnapshot.Gen0,
                    GC.CollectionCount(1) - lastSnapshot.Gen1,
                    GC.CollectionCount(2) - lastSnapshot.Gen2);
                if (reset)
                {
                    lastSnapshot = newSnapshot;
                }
                return newSnapshot;
            }
        }
    }

    public class GCStatsSnapshot
    {
        public GCStatsSnapshot(int gen0, int gen1, int gen2)
        {
            this.Gen0 = gen0;
            this.Gen1 = gen1;
            this.Gen2 = gen2;
        }

        public int Gen0 { get; }
        public int Gen1 { get; }
        public int Gen2 { get; }

        public override string ToString()
        {
            return string.Format("[GCCollectionCount: gen0 {0}, gen1 {1}, gen2 {2}]", Gen0, Gen1, Gen2);
        }
    }
}
