using System;
using System.Threading;

namespace N3
{
    public sealed class ThreadPooledWorkQueue : WorkQueue
    {
        private int _doWorking = 0;

#if NET6_0_OR_GREATER
    private readonly Action<object?> _callback;
#else
        private readonly WaitCallback _callback;
#endif

        public ThreadPooledWorkQueue()
        {
            _callback = ThreadPoolExecute;
        }

        protected override bool TryInlineExecute(SendOrPostCallback d, object? state)
        {
            return false;
        }

        protected override void OnPostWorkItem()
        {
            if (Interlocked.CompareExchange(ref _doWorking, 1, 0) == 0)
            {
#if NET6_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(_callback, this, false);
#else
                ThreadPool.UnsafeQueueUserWorkItem(_callback, this);
#endif
            }
        }

        private void ThreadPoolExecute(object? state)
        {
            while (true)
            {
                this.Process();

                _doWorking = 0;
                Thread.MemoryBarrier();
                if (this.IsEmpty)
                    break;
                if (Interlocked.Exchange(ref _doWorking, 1) == 1)
                    break;
            }
        }

        protected override void OnUpdate()
        {
        }
    }
}