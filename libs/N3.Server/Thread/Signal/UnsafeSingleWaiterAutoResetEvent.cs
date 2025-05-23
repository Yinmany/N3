using Cysharp.Threading.Tasks;

namespace N3;

/// <summary>
/// 只可在线程安全环境中使用
/// </summary>
public sealed class UnsafeSingleWaiterAutoResetEvent : IUniTaskSource
{
    private const byte STATE_N = 0; // 无
    private const byte STATE_S = 1; // 触发信号
    private const byte STATE_W = 2; // 触发等待

    private byte _status = STATE_N;

    private UniTaskCompletionSourceCore<bool> _waitSource;

    public void GetResult(short token)
    {
        _waitSource.GetResult(token);
        _waitSource.Reset();

        unchecked // 重置信号状态
        {
            _status = 0;
        }
    }

    public UniTaskStatus UnsafeGetStatus() => _waitSource.UnsafeGetStatus();

    public UniTaskStatus GetStatus(short token) => _waitSource.GetStatus(token);

    public void OnCompleted(Action<object> continuation, object state, short token) => _waitSource.OnCompleted(continuation, state, token);

    public void Signal()
    {
        _status |= STATE_S;
        if ((_status & STATE_W) == STATE_W)
            _waitSource.TrySetResult(true);
    }

    public UniTask WaitAsync()
    {
        if ((_status & STATE_W) == STATE_W)
            ThrowConcurrentWaitersNotSupported();

        if ((_status & STATE_S) == STATE_S) // 已经触发过了
        {
            _waitSource.TrySetResult(true);
        }

        _status |= STATE_W;
        return new UniTask(this, _waitSource.Version);
    }

    private static void ThrowConcurrentWaitersNotSupported() => throw new InvalidOperationException("不支持并发Wait");
}