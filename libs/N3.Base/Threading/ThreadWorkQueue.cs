using System;
using System.Threading;

namespace N3
{
    /// <summary>
    /// 使用单独线程执行的工作队列
    /// </summary>
    public sealed class ThreadWorkQueue : WorkQueue
    {
        private readonly int _threadId;

        public ThreadWorkQueue(int threadId)
        {
            this._threadId = threadId;
        }

        protected override bool TryInlineExecute(SendOrPostCallback d, object? state)
        {
            if (_threadId == Environment.CurrentManagedThreadId)
            {
                d(state);
                return true;
            }

            return false;
        }

        protected override void OnPostWorkItem()
        {
        }

        protected override void OnUpdate()
        {
        }

        public void Update() => Process();
    }
}