using System.Runtime.CompilerServices;

namespace N3;

/// <summary>
/// 异步锁池
///     Type=0默认由消息队列使用
/// </summary>
public sealed class XAsyncLockPool
{
    /// <summary>
    /// 初始64种
    /// </summary>
    private Dictionary<long, PooledXAsyncLock>[] _locks = new Dictionary<long, PooledXAsyncLock>[64];

    private Stack<PooledXAsyncLock> _pool = new Stack<PooledXAsyncLock>();

    public XAsyncLockPool()
    {
        for (int i = 0; i < _locks.Length; i++)
        {
            _locks[i] = new Dictionary<long, PooledXAsyncLock>();
        }
    }

    private PooledXAsyncLock Rent(ushort type, long token)
    {
        if (!_pool.TryPop(out var asyncLock))
        {
            asyncLock = new PooledXAsyncLock(this);
        }

        asyncLock.Type = type;
        asyncLock.Token = token;
        return asyncLock;
    }

    private void Return(PooledXAsyncLock asyncLock)
    {
        if (_pool.Count < 100000) // 池中最多同时存在10w个
        {
            _locks[asyncLock.Type].Remove(asyncLock.Token);
            asyncLock.Token = 0;
            _pool.Push(asyncLock);
        }
    }

    /// <summary>
    /// 等待锁
    /// </summary>
    /// <param name="type">锁的类型(最多65535种)</param>
    /// <param name="token">令牌</param>
    /// <returns></returns>
    public PooledXAsyncLock WaitAsync(ushort type, long token)
    {
        if (type >= _locks.Length)
        {
            int begin = _locks.Length;
            int end = type + 1;
            Array.Resize(ref _locks, Math.Min(end, ushort.MaxValue));

            for (int i = begin; i < end; i++)
            {
                _locks[i] = new Dictionary<long, PooledXAsyncLock>();
            }
        }

        var dic = _locks[type];
        if (!dic.TryGetValue(token, out var asyncLock))
        {
            asyncLock = Rent(type, token);
            dic.Add(token, asyncLock);
        }

        return asyncLock;
    }

    /// <summary>
    /// 取消异步锁中排队的异步延续回调
    /// </summary>
    /// <param name="type"></param>
    /// <param name="token"></param>
    /// <param name="e"></param>
    public void Cancel(ushort type, long token, Exception? e = null)
    {
        if (type < _locks.Length)
        {
            var dic = _locks[type];
            if (!dic.TryGetValue(token, out var asyncLock))
                return;

            // 取消掉，并放入池中。
            asyncLock.Cancel(e);
        }
    }

    public class PooledXAsyncLock : IDisposable
    {
        private readonly XAsyncLockPool _pool;
        private readonly Queue<Action> _queue = new Queue<Action>();


        private int _num;
        private Exception? _exception;

        internal ushort Type { get; set; }
        internal long Token { get; set; }

        internal PooledXAsyncLock(XAsyncLockPool pool)
        {
            _pool = pool;
        }

        public Awaiter GetAwaiter() => new(this);

        /// <summary>
        /// 在调用Dispose时有两种情况: 1.没有异步逻辑，排队就不存在。 2.有异步逻辑，dispose就会在同步上下文中进行调用。都不会存在递归调用。
        /// </summary>
        public void Dispose() // 释放一次锁
        {
            --_num;
            if (_queue.TryDequeue(out Action? continuation))
            {
                continuation();
            }
            else // 已经没有任何异步持有锁了
            {
                _pool.Return(this);
            }
        }

        internal void Cancel(Exception? e)
        {
            _exception = e;
            _num = 0;
            _queue.Clear();
            _pool.Return(this);
        }

        public readonly struct Awaiter : INotifyCompletion
        {
            private readonly PooledXAsyncLock _xAsyncLock;

            public bool IsCompleted => false;

            public IDisposable GetResult() => _xAsyncLock;

            public Awaiter(PooledXAsyncLock xAsyncLock)
            {
                _xAsyncLock = xAsyncLock;
            }

            public void OnCompleted(Action continuation)
            {
                ++_xAsyncLock._num;
                if (_xAsyncLock._num is 1) // 第一个不用排队，需要马上执行。
                {
                    continuation();
                }
                else
                {
                    _xAsyncLock._queue.Enqueue(continuation);
                }
            }
        }
    }
}