using N3.Network;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;

namespace N3;

/// <summary>
/// 消息接收者
/// </summary>
public interface IMessageReceiver
{
    /// <summary>
    /// 线程不安全的
    /// </summary>
    /// <param name="fromNodeId"></param>
    /// <param name="message"></param>
    void OnUnsafeReceive(ushort fromNodeId, IMessage message);
}

public interface IMessageCenter
{
    void Listen(IPEndPoint bindIp);
    void AddNode(ushort nodeId, IPEndPoint ip);
    bool RemoveNode(ushort id, bool disconnect = true);
    void AddReceiver(long id, IMessageReceiver receiver);

    void RemoveReceiver(long id);

    /// <summary>
    /// 订阅连接断开
    /// </summary>
    /// <returns></returns>
    IDisposable SubscribeDisconnect(Action<ushort, bool> callback);

    bool Send(long id, IMessage msg);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="req"></param>
    /// <param name="timeout">超时默认60s(-1不超时)</param>
    /// <returns></returns>
    ValueTask<IResponse> Call(long id, IRequest req, short timeout = 60);
}

/// <summary>
/// 消息中心
/// </summary>
public partial class MessageCenter : WorkQueue, IMessageCenter, IThreadPoolWorkItem
{
    public static IMessageCenter Ins { get; } = new MessageCenter();

    private readonly SocketSchedulers _socketSchedulers = new(false, 1);

    private readonly ConcurrentDictionary<long, IMessageReceiver> _receivers = new();
    private readonly ConcurrentQueue<(long, object)> _sendQueue = new(); // 发送队列

    private readonly Dictionary<ushort, ClientSession> _sessions = new();
    private readonly Dictionary<int, ResponseTcs> _callbacks = new();
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly SendOrPostCallback _rspCallback;
    private readonly SendOrPostCallback _timeoutCheckCallback;
    private readonly List<DisconnectSubscribe> _disconnectSubscribes = new List<DisconnectSubscribe>();

    private int _rpcIdGen = 0;
    private int _doWorking = 0;

    private MessageCenter()
    {
        _rspCallback = OnResponse;
        _timeoutCheckCallback = OnTimeoutCheck;
        TimerMgr.Ins.AddInterval(TimeSpan.FromSeconds(5), OnTimeout);
    }

    public void Listen(IPEndPoint bindIp)
    {
        SLog.Info($"listen {bindIp}...");
        TcpConnListener listener = new TcpConnListener(_socketSchedulers);
        listener.Listen(bindIp.Port, bindIp.Address);
        _ = RunAsync(_shutdownCts.Token);
        return;

        async Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                TcpConn? conn = await listener.AcceptAsync(cancellationToken);
                if (conn is null)
                    break;
                conn.Handler = this;
                CancellationTokenRegistration registration = cancellationToken.Register(state => { _ = ((TcpConn)state!).CloseAsync(); }, conn);
                conn.ClosedToken.Register(() => registration.Dispose());
                conn.Start();
            }
        }
    }

    private ClientSession? GetSession(ushort nodeId)
    {
        _sessions.TryGetValue(nodeId, out var session);
        return session;
    }

    private void OnRpcError(int rpcId, RpcException e)
    {
        if (_callbacks.TryGetValue(rpcId, out var tcs))
        {
            tcs.SetException(e);
        }
    }

    private void OnTimeout(TimerInfo handle) => Post(_timeoutCheckCallback, null);

    private void OnTimeoutCheck(object? state)
    {
        foreach (var s in _sessions.Values)
        {
            s.CheckTimeout();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PostSendQueue(long id, object state)
    {
        _sendQueue.Enqueue((id, state));
        TryExecute();
    }

    public void AddNode(ushort nodeId, IPEndPoint ip)
    {
        this.Post(_ =>
        {
            if (_sessions.TryGetValue(nodeId, out var session))
            {
                SLog.Info($"node ip change {nodeId} {ip}");
                session.ChangeIp(ip);
            }
            else
            {
                SLog.Info($"add node {nodeId} {ip}");
                _sessions.Add(nodeId, new ClientSession(nodeId, ip, _socketSchedulers.GetScheduler(), OnRpcError, this));
            }
        }, null);
    }

    public bool RemoveNode(ushort id, bool disconnect = true)
    {
        Post(_ =>
        {
            if (!_sessions.Remove(id, out var session))
                return;

            SLog.Info($"remove node {id}");
            if (disconnect)
            {
                session.Dispose();
            }
        }, null);
        return true;
    }

    public void AddReceiver(long id, IMessageReceiver receiver)
    {
        _receivers.TryAdd(id, receiver);
    }

    public void RemoveReceiver(long id)
    {
        _receivers.TryRemove(id, out _);
    }

    internal class DisconnectSubscribe(Action<ushort, bool> callback) : IDisposable
    {
        private Action<ushort, bool>? _callback = callback;
        private SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;

        public bool IsDisposed => Volatile.Read(ref _callback) == null;

        public void Invoke(ushort nodeId, bool isLocal) => _callback?.Invoke(nodeId, isLocal);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _callback, null) != null)
            {
                MessageCenter mc = (MessageCenter)Ins;
                mc.Post(_ => { mc._disconnectSubscribes.Remove(this); }, null);
            }
        }
    }


    public IDisposable SubscribeDisconnect(Action<ushort, bool> callback)
    {
        DisconnectSubscribe disconnectSubscribe = new DisconnectSubscribe(callback);
        Post(_ =>
        {
            if (disconnectSubscribe.IsDisposed)
                return;
            _disconnectSubscribes.Add(disconnectSubscribe);
        }, null);
        return disconnectSubscribe;
    }

    public bool Send(long id, IMessage msg)
    {
        PostSendQueue(id, msg);
        return true;
    }

    public ValueTask<IResponse> Call(long id, IRequest req, short timeout = 60)
    {
        Did idInfo = _rpcIdGen;
        ResponseTcs tcs = ResponseTcs.Create(req, idInfo.NodeId, timeout);
        PostSendQueue(id, tcs);
        return tcs.Task;
    }
}