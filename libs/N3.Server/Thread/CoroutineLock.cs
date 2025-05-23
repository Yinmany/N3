using System.Runtime.CompilerServices;

namespace N3;

/// <summary>
/// 协程锁(必须在线程安全环境下使用)
/// </summary>
public class CoroutineLock
{
    private readonly Queue<Action> _queue = new();

    /// <summary>
    /// 最近锁定时间
    /// </summary>
    public long? LastLockTime { get; private set; }

    /// <summary>
    /// 是否已锁定
    /// </summary>
    public bool IsLocked => _queue.Count != 0;

    public Awaiter EnterAsync() => new Awaiter(this);

    /// <summary>
    /// 版本号(取消后版本+1)
    /// </summary>
    public uint Version { get; private set; } = 1;

    private void Exit()
    {
        if (_queue.TryDequeue(out _) && _queue.TryPeek(out Action? next))
        {
            LastLockTime = STime.NowMs;
            next();
        }
        else
        {
            LastLockTime = null; // 没有锁定了
        }
    }

    /// <summary>
    /// 取消所有等待等待
    /// </summary>
    /// <param name="e"></param>
    public void Cancel()
    {
        unchecked
        {
            Version++;
        }

        int count = _queue.Count;
        while (count > 0)
        {
            count--;
            Exit();
        }
        _queue.Clear();
    }

    public readonly struct Awaiter(CoroutineLock ctx) : INotifyCompletion, IDisposable
    {
        public readonly uint Versions = ctx.Version;

        public Awaiter GetAwaiter() => this;

        public bool IsCompleted => false;

        public Awaiter GetResult()
        {
            if (Versions != ctx.Version)
                throw new OperationCanceledException();
            return this;
        }

        public void OnCompleted(Action continuation)
        {
            var queue = ctx._queue;
            queue.Enqueue(continuation);
            if (queue.Count == 1) // 第一个不用排队，需要马上执行。
            {
                continuation();
                ctx.LastLockTime = STime.NowMs;
            }
        }

        /// <summary>
        /// 在调用Dispose时有两种情况: 1.没有异步逻辑，排队就不存在。 2.有异步逻辑，dispose就会在同步上下文中进行调用。都不会存在递归调用
        /// </summary>
        public void Dispose()
        {
            ctx.Exit();
        }
    }
}