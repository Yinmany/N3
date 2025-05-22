using System.Collections.Concurrent;

namespace N3Core;

/// <summary>
/// 单线程模型的同步上下文
/// </summary>
public class SingleThreadSynchronizationContext : SynchronizationContext
{
    private readonly WorkThread? _workThread;

    private int _doWorking = 0;
    private bool _isDisposed = false;

    private readonly ConcurrentQueue<WorkItem> _queue = new();
    private readonly Action<object>? _callback;

    readonly struct WorkItem(SendOrPostCallback callback, object? state)
    {
        public void Execute() => callback(state);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="workThread">null:使用线程池执行</param>
    protected SingleThreadSynchronizationContext(WorkThread? workThread = null)
    {
        _workThread = workThread;
        if (workThread is null)
        {
            _callback = ThreadPoolExecute;
            return;
        }

        workThread.OnTick += Tick;
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        _queue.Enqueue(new WorkItem(d, state));
        if (_callback != null && Interlocked.CompareExchange(ref _doWorking, 1, 0) == 0)
        {
            ThreadPool.UnsafeQueueUserWorkItem(_callback, this, false);
        }
    }

    /// <summary>
    /// 1.线程池模式,每次执行queue时将会触发一次
    /// 2.workThread模式,每帧都会触发
    /// </summary>
    protected virtual void OnTick()
    {
    }

    private void Tick()
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
                    SLog.Error(e, "e:");
                }
            }

            OnTick();
        }
        finally
        {
            SetSynchronizationContext(oldSynchronizationContext);
        }
    }

    private void ThreadPoolExecute(object? state)
    {
        while (true)
        {
            this.Tick();

            _doWorking = 0;
            Thread.MemoryBarrier();
            if (_queue.IsEmpty)
                break;

            if (Interlocked.Exchange(ref _doWorking, 1) == 1)
                break;
        }
    }

    protected void Dispose()
    {
        if (this._isDisposed)
            return;
        this._isDisposed = true;

        if (_workThread != null)
            _workThread.OnTick -= Tick;
    }
}