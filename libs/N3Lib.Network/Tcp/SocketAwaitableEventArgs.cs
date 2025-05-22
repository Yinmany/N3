using System.Net.Sockets;
using System.Threading.Tasks.Sources;

namespace N3Lib.Network;

internal class SocketAwaitableEventArgs : SocketAsyncEventArgs, IValueTaskSource<SocketOperationResult>
{
    private readonly IOQueue _ioQueue;
    private readonly Action<object> _callback;

    private ManualResetValueTaskSourceCore<SocketOperationResult> _core;

    public SocketAwaitableEventArgs(IOQueue ioQueue)
#if !NETSTANDARD2_1_OR_GREATER
        : base(unsafeSuppressExecutionContextFlow: true)
#endif
    {
        _ioQueue = ioQueue;
        _callback = OnCompleted;
    }

    protected override void OnCompleted(SocketAsyncEventArgs e)
    {
        _ioQueue.Schedule(_callback, e);
    }

    private void OnCompleted(object state)
    {
        SocketAsyncEventArgs e = (SocketAwaitableEventArgs)state;
        if (e.SocketError != SocketError.Success)
        {
            _core.SetResult(new SocketOperationResult(CreateException(e.SocketError)));
        }
        else
        {
            _core.SetResult(new SocketOperationResult(e.BytesTransferred));
        }
    }

    protected static SocketException CreateException(SocketError e)
    {
        return new SocketException((int)e);
    }

    public SocketOperationResult GetResult(short token)
    {
        try
        {
            return _core.GetResult(token);
        }
        finally
        {
            _core.Reset();
        }
    }

    ValueTaskSourceStatus IValueTaskSource<SocketOperationResult>.GetStatus(short token)
    {
        return _core.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _core.OnCompleted(continuation, state, token, flags);
    }
}
