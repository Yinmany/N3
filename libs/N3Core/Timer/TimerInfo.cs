namespace N3Core;

public class TimerInfo : IDisposable
{
    /// <summary>
    /// 最终超时时间
    /// </summary>
    public long Time { get; private set; }

    private readonly uint _timeoutMs;
    private readonly Action<TimerInfo> _callback;
    private bool _isDisposed;

    public TimerInfo? Next, Prev;
    public readonly object? State;

    internal TimerInfo(uint timeoutMs, bool isInterval, Action<TimerInfo> callback, int type, object? state)
    {
        _timeoutMs = timeoutMs;
        IsInterval = isInterval;
        Type = type;
        State = state;
        _callback = callback;

        Time = STime.NowMs + _timeoutMs;
        _isDisposed = false;
    }

    internal void UpdateTime()
    {
        Time = STime.NowMs + _timeoutMs;
    }

    internal void Trigger()
    {
        _callback.Invoke(this);
    }

    public int Type { get; }

    public bool IsInterval { get; }

    public bool IsDisposed => Volatile.Read(ref _isDisposed);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, true))
            return;
        TimerMgr.Ins.Remove(this);
    }
}