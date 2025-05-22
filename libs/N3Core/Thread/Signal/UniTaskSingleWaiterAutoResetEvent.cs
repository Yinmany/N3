using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace N3Core;

/// <summary>
/// 线程安全的单个等待者自动重置事件
/// </summary>
public sealed class UniTaskSingleWaiterAutoResetEvent : IUniTaskSource
{
    // 0001
    private const uint SignaledFlag = 1;

    // 0010
    private const uint WaitingFlag = 1 << 1;

    // 1110 & 1101 = 1100
    private const uint ResetMask = ~SignaledFlag & ~WaitingFlag;

    private volatile uint _status;
    private UniTaskCompletionSourceCore<bool> _waitSource;
    private readonly SynchronizationContext? _synchronizationContext;
    private readonly SendOrPostCallback _setResultCallback;

    public UniTaskSingleWaiterAutoResetEvent()
    {
        _setResultCallback = SetResult;
    }

    /// <summary>
    /// 指定同步上下文
    /// </summary>
    /// <param name="synchronizationContext"></param>
    public UniTaskSingleWaiterAutoResetEvent(SynchronizationContext synchronizationContext) : this()
    {
        _synchronizationContext = synchronizationContext;
    }

    public void GetResult(short token)
    {
        _waitSource.GetResult(token);
        _waitSource.Reset();
        ResetStatus();
    }

    public UniTaskStatus UnsafeGetStatus() => _waitSource.UnsafeGetStatus();
    public UniTaskStatus GetStatus(short token) => _waitSource.GetStatus(token);
    public void OnCompleted(Action<object> continuation, object state, short token) => _waitSource.OnCompleted(continuation, state, token);
    private static void ThrowConcurrentWaitersNotSupported() => throw new InvalidOperationException("不支持并发Wait");

    private void SetResult(object? _) => _waitSource.TrySetResult(true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniTask WaitAsync()
    {
        var status = Interlocked.Or(ref _status, WaitingFlag);

        // 状态之前以及处于等待时，抛出异常；不支持并发等待
        if ((status & WaitingFlag) == WaitingFlag)
        {
            ThrowConcurrentWaitersNotSupported();
        }

        // 之前已经有信号了，那么直接触发
        if ((status & SignaledFlag) == SignaledFlag)
        {
            // 仅重置状态，因为 `_waitSource` 尚未被设置；GetResult不会触发
            ResetStatus();
            return default;
        }

        return new UniTask(this, _waitSource.Version);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Signal()
    {
        if ((_status & SignaledFlag) == SignaledFlag)
        {
            // 已经存在信号，直接返回
            return;
        }

        var status = Interlocked.Or(ref _status, SignaledFlag);
        // 因为多线程的原因，status可能已经被设置，此时需要判断是否需要触发
        if ((status & SignaledFlag) != SignaledFlag && (status & WaitingFlag) == WaitingFlag)
        {
            Debug.Assert((_status & (SignaledFlag | WaitingFlag)) == (SignaledFlag | WaitingFlag));
            if (_synchronizationContext != null)
            {
                _synchronizationContext.Post(_setResultCallback, this);
            }
            else
            {
                _waitSource.TrySetResult(true);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResetStatus()
    {
        // 事件正在处理中，因此现在清除“Signaled”标志。
        // 等待者不再等待，因此也清除“Waiting”标志。
        var status = Interlocked.And(ref _status, ResetMask);

        // 如果“Waiting”和“Signaled”标志都没有被设置，那么说明出现了严重错误。
        Debug.Assert((status & (WaitingFlag | SignaledFlag)) == (WaitingFlag | SignaledFlag));
    }
}