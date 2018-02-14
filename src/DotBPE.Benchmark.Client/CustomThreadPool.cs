using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotBPE.Benchmark.Client
{
    public class CustomThreadPool
    {
        private int _size;
     
        private IWorkItemFactory _workItemFactory;
        private CancellationTokenSource _cancellationTokenSource;
        private List<Thread> _threadPool = new List<Thread>();
        private int _countOfOperations;
        private int _total;
        private bool _isWarmup = false;
        public event EventHandler<WorkItemFinishedEventArgs> WorkItemFinished;
        public event EventHandler<EventArgs> WarmupFinished;


        public class WorkItemFinishedEventArgs : EventArgs
        {
            public WorkItemFinishedEventArgs(WorkResult result)
            {
                Result = result;
            }

            public WorkResult Result { get; private set; }
        }

        public int WorkerCount
        {
            get
            {
                return _threadPool.Count;
            }
        }


        public CustomThreadPool(IWorkItemFactory workItemFactory,
            CancellationTokenSource tokenSource,
            int size = 100, int warmUpSeconds = 0)
        {
            _workItemFactory = workItemFactory;
            _size = size;
            _cancellationTokenSource = tokenSource;

            if (warmUpSeconds > 0)
            {
                _isWarmup = true;
                // DONT' AWAIT !!!
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                Warmup(TimeSpan.FromSeconds(Math.Max(warmUpSeconds / size, 1)));
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
            }
            else
            {
                for (int i = 0; i < _size; i++)
                    AddThread();
            }

        }
        private async Task Warmup(TimeSpan interval)
        {
            _isWarmup = true;
            for (int i = 0; i < _size; i++)
            {
                AddThread(); 
                await Task.Delay(interval);
            }

            _isWarmup = false;
            OnWarmupFinished(new EventArgs());
        }

        private void AddThread()
        {
            var thread = new Thread(() => LoopAsync(_cancellationTokenSource.Token).Wait());
            _threadPool.Add(thread);
            thread.Start();
        }


        protected void OnWarmupFinished(EventArgs e)
        {
            if (WarmupFinished != null)
                WarmupFinished(this, e);

        }

        protected void OnWorkItemFinished(WorkItemFinishedEventArgs eventArgs)
        {
            if (WorkItemFinished != null)
                WorkItemFinished(this, eventArgs);
        }


        public void Start(int countOfOperations)
        {
            _countOfOperations = countOfOperations;

            _threadPool.ForEach((a) =>
            {
                if (a.ThreadState == System.Threading.ThreadState.Unstarted)
                    a.Start();
            });
        }

        private async Task LoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    var workItem = _workItemFactory.GetWorkItem(_isWarmup);
                    var result = await workItem;
                    if (result.NoWork)
                    {                    
                        break;
                    } 
                       
                    stopwatch.Stop();
                    OnWorkItemFinished(new WorkItemFinishedEventArgs(result));
                }
                catch (Exception e) // THIS PATH REALLY SHOULD NEVER HAPPEN !!
                {
                    stopwatch.Stop();
                    Console.WriteLine(e);
                    OnWorkItemFinished(new WorkItemFinishedEventArgs(new WorkResult()
                    {
                        Status = -1,
                        Index = -1,                    
                        Ticks = stopwatch.ElapsedTicks,
                        IsWarmUp = _isWarmup
                    }));
                }
              
                if (!_isWarmup)
                {
                    var total = Interlocked.Increment(ref _total);                  
                    if (_total >= _countOfOperations)
                        _cancellationTokenSource.Cancel();
                }
            }
        }


        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
