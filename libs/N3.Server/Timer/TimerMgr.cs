using System.Collections.Concurrent;

namespace N3;

public class TimerMgr : Singleton<TimerMgr>
{
    private const uint UnitMs = 100;

    private readonly ConcurrentQueue<(bool isAdd, TimerInfo timer)> _cmdQueue = new();
    private readonly SortedDictionary<long, TimerInfo> _timerDic = new(); // 定时器字典
    private readonly Queue<TimerInfo> _timeoutQueue = new Queue<TimerInfo>(); // 定时器超时队列
    private long _minTime = long.MaxValue; // 记录最小时间
    private readonly Lock _lock = new Lock();

    private TimerMgr()
    {
        // 100ms 一下
        Timer timer = new Timer(OnTimer, null, 0, UnitMs);
    }

    private void OnTimer(object? state)
    {
        lock (_lock) // 锁一下，如果执行超过 UnitMs，会触发一次，另一个线程会进入
        {
            ProcessCmdQueue();
            ProcessTimeout();
        }
    }

    private void ProcessCmdQueue()
    {
        while (_cmdQueue.TryDequeue(out var cmd))
        {
            (bool isAdd, TimerInfo timer) = cmd;

            if (isAdd)
            {
                if (timer.IsDisposed)
                    continue;
                AddToTimerDic(timer);
            }
            else
            {
                long tillTime = timer.Time;
                if (!_timerDic.TryGetValue(tillTime, out TimerInfo? timerInfo))
                    continue;

                // 只有一个的情况
                if (timer.Prev != null)
                    timer.Prev.Next = timer.Next;

                if (timer.Next != null)
                {
                    timer.Next.Prev = timer.Prev;
                }
                else // 尾部元素被移除
                {
                    if (timer.Prev is null) // 前面也没元素，就移除掉字典
                    {
                        _timerDic.Remove(tillTime);
                    }
                    else
                    {
                        _timerDic[tillTime] = timer.Prev; // 更新字典为尾部元素
                    }
                }
            }
        }
    }

    private void AddToTimerDic(TimerInfo timer)
    {
        long tillTime = timer.Time;
        if (_minTime > tillTime)
            _minTime = tillTime;

        if (!_timerDic.TryGetValue(tillTime, out TimerInfo? timerInfo))
        {
            _timerDic.Add(tillTime, timer);
        }
        else // 追加
        {
            timerInfo.Next = timer;
            timer.Prev = timerInfo;
            _timerDic[tillTime] = timer; // 字典永远记录尾部对象
        }
    }

    private void ProcessTimeout()
    {
        if (_timerDic.Count == 0)
            return;

        long nowMs = STime.NowMs;
        if (_minTime > nowMs) // 没有可用处理的定时器
            return;

        foreach (var kv in _timerDic)
        {
            long timeout = kv.Key; // 最终超时时间
            if (timeout > nowMs)
            {
                _minTime = timeout;
                break;
            }

            _timeoutQueue.Enqueue(kv.Value);
        }

        while (_timeoutQueue.TryDequeue(out var timer))
        {
            _timerDic.Remove(timer.Time); // 移除到时间的定时器

            // 从尾部向前遍历触发
            TimerInfo? cur = timer;
            while (cur != null)
            {
                // 触发cur
                timer.Trigger();

                var curTmp = cur;
                cur = cur.Prev;

                // 重置一下
                curTmp.Prev = curTmp.Next = null;
                if (curTmp.IsInterval) // 重新放入
                {
                    curTmp.UpdateTime();
                    AddToTimerDic(curTmp);
                }
            }
        }

        if (_timerDic.Count == 0)
            _minTime = long.MaxValue;
    }

    internal void Remove(TimerInfo timer)
    {
        _cmdQueue.Enqueue((false, timer));
    }

    public TimerInfo AddTimeout(TimeSpan dueTime, Action<TimerInfo> action, int type = 0, object? state = null)
    {
        uint timeoutMs = (uint)dueTime.TotalMilliseconds;
        TimerInfo info = new TimerInfo(timeoutMs, false, action, type, state);
        _cmdQueue.Enqueue((true, info));
        return info;
    }

    public TimerInfo AddInterval(TimeSpan period, Action<TimerInfo> action, int type = 0, object? state = null)
    {
        uint timeoutMs = (uint)period.TotalMilliseconds;
        TimerInfo info = new TimerInfo(timeoutMs, true, action, type, state);
        _cmdQueue.Enqueue((true, info));
        return info;
    }
}