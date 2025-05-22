using System.Threading.Tasks.Sources;

namespace N3Core;

/// <summary>
/// 使用 ValueTaskSource 的实现(因为需要捕获同步上下文)
/// </summary>
public class ResponseTcs : IValueTaskSource<IResponse>, ISObjectPoolNode<ResponseTcs>
{
    private static SObjectPool<ResponseTcs> pool;
    private ResponseTcs? _nextNode;

    ref ResponseTcs? ISObjectPoolNode<ResponseTcs>.NextNode => ref _nextNode;

    private ManualResetValueTaskSourceCore<IResponse> _core;

    public ValueTask<IResponse> Task => new(this, _core.Version);

    public long SendTime { get; private set; }

    public IRequest Request { get; private set; }

    public ushort ReqNodeId { get; private set; }

    public short Timeout { get; private set; }

    public IResponse GetResult(short token)
    {
        try
        {
            return _core.GetResult(token);
        }
        finally
        {
            SendTime = 0;
            Request = null;
            ReqNodeId = 0;
            this.Timeout = 0;
            _core.Reset();
            pool.TryPush(this);
        }
    }

    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _core.OnCompleted(continuation, state, token, flags);
    }

    public void SetResult(IResponse response) => _core.SetResult(response);
    public void SetException(Exception exception) => _core.SetException(exception);

    public static ResponseTcs Create(IRequest req, ushort nodeId, short timeout)
    {
        if (!pool.TryPop(out var result)) // pool中没有，就new一个
            result = new ResponseTcs();
        result!.SendTime = STime.NowMs;
        result.Request = req;
        result.ReqNodeId = nodeId;
        result.Timeout = timeout;
        return result!;
    }
}