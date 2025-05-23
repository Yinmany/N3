using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace N3.Network;

public class IOQueue : PipeScheduler
{
    private readonly ConcurrentQueue<(Action<object?> callback, object? state)> _workItems = new();

    private int _doingWork;

#if NETSTANDARD2_1_OR_GREATER
    private readonly WaitCallback executeCallback;
#else
    private readonly Action<object?> executeCallback;
#endif

    private Thread? _thread;

    public IOQueue(bool isThread = false)
    {
        executeCallback = this.Execute;
        if (isThread)
        {
            _thread = new Thread(Update);
            _thread.IsBackground = true;
            _thread.Start();
        }
    }

    public override void Schedule(Action<object?> action, object? state)
    {
        _workItems.Enqueue((action, state));
        if (_thread == null && Interlocked.CompareExchange(ref _doingWork, 1, 0) == 0)
        {
#if NETSTANDARD2_1_OR_GREATER
            _ = System.Threading.ThreadPool.UnsafeQueueUserWorkItem(executeCallback, null);
#else
            _ = System.Threading.ThreadPool.UnsafeQueueUserWorkItem(executeCallback, null, preferLocal: false);
#endif
        }
    }

    private void Update()
    {
        while (true)
        {
            Thread.Sleep(1);

            while (_workItems.TryDequeue(out var item))
            {
                item.callback.Invoke(item.state);
            }
        }
    }

    private void Execute(object? state)
    {
        while (true)
        {
            while (_workItems.TryDequeue(out var item))
            {
                item.callback.Invoke(item.state);
            }

            _doingWork = 0;
            Thread.MemoryBarrier(); // 内存屏障,读取集合最新的Count(其它线程放入了,但此线程看不见)
            if (_workItems.IsEmpty)
                break;

            // 改变成1,如果之前还是1说明在放入时就已经调度起来了.可以跳出了
            if (Interlocked.Exchange(ref _doingWork, 1) == 1)
                break;
        }
    }
}