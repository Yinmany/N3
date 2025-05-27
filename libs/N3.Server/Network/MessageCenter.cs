using N3.Network;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;

namespace N3;

/// <summary>
/// 节点网络状态
/// </summary>
public enum NodeNetworkState
{
    Connected,
    Disconnected
}

/// <summary>
/// 消息接收者
/// </summary>
public interface IMessageReceiver
{
    /// <summary>
    /// 线程不安全的
    /// </summary>
    /// <param name="fromNodeId">消息来之的节点id</param>
    /// <param name="message">消息</param>
    void OnUnsafeReceive(ushort fromNodeId, IMessage message);

    /// <summary>
    /// 节点网络状态
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="status">状态</param>
    /// <param name="isClientSide">是否是当前节点发出的连接(Client)，否则就是收到连接(Server)</param>
    void OnUnsafeNodeNetworkStatus(ushort nodeId, NodeNetworkState status, bool isClientSide);
}

public interface IMessageCenter
{
    void Listen(IPEndPoint bindIp);
    void AddNode(ushort nodeId, IPEndPoint ip);
    bool RemoveNode(ushort id, bool disconnect = true);
    void AddReceiver(long id, IMessageReceiver receiver);

    void RemoveReceiver(long id);

    bool Send(long id, IMessage msg);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="req"></param>
    /// <param name="timeout">超时默认60s(-1不超时)</param>
    /// <returns></returns>
    ValueTask<IResponse> Call(long id, IRequest req, short timeout = 60);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRsp"></typeparam>
    /// <param name="id"></param>
    /// <param name="req"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    ValueTask<TRsp> Call<TRsp>(long id, IRequest req, short timeout = 60) where TRsp : class, IResponse;
}

/// <summary>
/// 消息中心
/// </summary>
public partial class MessageCenter : IMessageCenter
{
    public static IMessageCenter Ins { get; } = new MessageCenter();

    private readonly InternalWorkQueue _workQueue;
    private readonly SocketSchedulers _socketSchedulers = new(false, 1);

    private readonly ConcurrentDictionary<long, IMessageReceiver> _receivers = new();
    private readonly ConcurrentQueue<(long, object)> _sendQueue = new(); // 发送队列

    private readonly Dictionary<ushort, ClientSession> _sessions = new();
    private readonly Dictionary<int, ResponseTcs> _callbacks = new();
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly SendOrPostCallback _rspCallback;
    private readonly SendOrPostCallback _timeoutCheckCallback;
    private readonly RpcTimeoutQueue _innerTimeoutQueue;

    private int _rpcIdGen = 0;

    private static SLogger logger = new SLogger(nameof(MessageCenter));

    private MessageCenter()
    {
        _workQueue = new InternalWorkQueue(this);
        _rspCallback = OnResponse;
        _timeoutCheckCallback = OnTimeoutCheck;
        _innerTimeoutQueue = new RpcTimeoutQueue(_callbacks);

        TimerMgr.Ins.AddInterval(TimeSpan.FromSeconds(5), OnTimeout);
    }

    public void Listen(IPEndPoint bindIp)
    {
        logger.Info($"listen {bindIp}...");
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
                conn.ClosedToken.Register(registration.Dispose);
                conn.Start();
            }
        }
    }

    private ClientSession? GetSession(ushort nodeId)
    {
        _sessions.TryGetValue(nodeId, out var session);
        return session;
    }

    private void OnTimeout(TimerInfo handle) => _workQueue.Post(_timeoutCheckCallback, null);

    private void OnTimeoutCheck(object? state)
    {
        _innerTimeoutQueue.CheckTimeout();
        foreach (var s in _sessions.Values)
        {
            s.timeoutQueue.CheckTimeout();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PostSendQueue(long id, object state)
    {
        _sendQueue.Enqueue((id, state));
        _workQueue.TryExecute();
    }

    public void AddNode(ushort nodeId, IPEndPoint ip)
    {
        _workQueue.Post(_ =>
        {
            if (_sessions.TryGetValue(nodeId, out var session))
            {
                session.ChangeIp(ip);
            }
            else
            {
                logger.Info($"add node {nodeId} {ip}");
                _sessions.Add(nodeId, new ClientSession(nodeId, ip, _socketSchedulers.GetScheduler(), this, this._callbacks));
            }
        }, null);
    }

    public bool RemoveNode(ushort id, bool disconnect = true)
    {
        _workQueue.Post(_ =>
        {
            if (!_sessions.Remove(id, out var session))
                return;

            logger.Info($"remove node {id}");
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

    public bool Send(long id, IMessage msg)
    {
        PostSendQueue(id, msg);
        return true;
    }

    public ValueTask<IResponse> Call(long id, IRequest req, short timeout = 60)
    {
        Did idInfo = id;
        ResponseTcs tcs = ResponseTcs.Create(req, idInfo.NodeId, timeout);
        PostSendQueue(id, tcs);
        return tcs.Task;
    }

    public async ValueTask<TRsp> Call<TRsp>(long id, IRequest req, short timeout = 60) where TRsp : class, IResponse
    {
        return (TRsp)await this.Call(id, req, timeout);
    }
}