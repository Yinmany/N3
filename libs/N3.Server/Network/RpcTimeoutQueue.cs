namespace N3;
public partial class MessageCenter
{
    internal sealed class RpcTimeoutQueue
    {
        private readonly Queue<(int, long)> _queue = new();

        private readonly IReadOnlyDictionary<int, ResponseTcs> _callbacks;

        public RpcTimeoutQueue(IReadOnlyDictionary<int, ResponseTcs> rpcCallbacks)
        {
            _callbacks = rpcCallbacks;
        }

        public void Enqueue(int rpcId, short timeout)
        {
            if (timeout < 0)
                return;
            _queue.Enqueue((rpcId, STime.NowMs + timeout * 1000)); // 只有发送了的，才会进入超时列表
        }

        public void CheckTimeout()
        {
            while (_queue.TryPeek(out var item))
            {
                if (item.Item2 > STime.NowMs)
                {
                    // 回调已经回调过了，就从超时队列里面移除掉
                    if (!_callbacks.ContainsKey(item.Item1))
                    {
                        _queue.Dequeue();
                        continue;
                    }
                    break; // 最开始的都还没超时，就跳出
                }

                _queue.Dequeue();
                if (_callbacks.TryGetValue(item.Item1, out var tcs))
                {
                    tcs.SetException(RpcException.Timeout);
                }
            }
        }

        public void Clear(RpcException ex)
        {
            // 清理一下rpc回调
            while (_queue.TryDequeue(out var item))
            {
                if (_callbacks.TryGetValue(item.Item1, out var tcs))
                {
                    tcs.SetException(ex);
                }
            }
        }
    }
}