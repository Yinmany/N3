using System;
using System.Collections.Concurrent;
using System.Threading;

namespace N3Lib
{
    /// <summary>
    /// 单线程模型的工作队列
    /// </summary>
    public abstract class WorkQueue : SynchronizationContext
    {
        private readonly ConcurrentQueue<WorkItem> _queue = new();

        public bool IsEmpty => _queue.IsEmpty;

        readonly struct WorkItem
        {
            private readonly SendOrPostCallback callback;
            private readonly object? state;

            public WorkItem(SendOrPostCallback callback, object? state)
            {
                this.callback = callback;
                this.state = state;
            }

            public void Execute() => callback(state);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            // 内联执行了
            if (TryInlineExecute(d, state))
                return;
            _queue.Enqueue(new WorkItem(d, state));
            OnPostWorkItem();
        }

        public void PostNext(SendOrPostCallback d, object? state)
        {
            _queue.Enqueue(new WorkItem(d, state));
            OnPostWorkItem();
        }

        protected abstract bool TryInlineExecute(SendOrPostCallback d, object? state);

        protected abstract void OnPostWorkItem();

        protected abstract void OnUpdate();

        protected void Process()
        {
            SynchronizationContext? oldSynchronizationContext = Current;
            SetSynchronizationContext(this);
            try
            {
                while (_queue.TryDequeue(out WorkItem workItem))
                {
                    try
                    {
                        workItem.Execute();
                    }
                    catch (Exception e)
                    {
                        SLog.Error(e, "WorkQueue:");
                    }
                }

                OnUpdate();
            }
            finally
            {
                SetSynchronizationContext(oldSynchronizationContext);
            }
        }
    }
}