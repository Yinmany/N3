using Cysharp.Threading.Tasks;
using MongoDB.Bson;
using N3;
using N3.Buffer;
using N3.Network;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace ProjectX;

/// <summary>
/// 客户端连接的网络
/// </summary>
public class NetworkComp : AComponent, INetworkCallback
{
    private static readonly SLogger Logger = new SLogger(nameof(NetworkComp));
    private static readonly INetwork _network = new TcpAndWsNetwork(true, 1);
    private readonly List<IDisposable> _listener = new();
    private readonly ConcurrentQueue<(uint, IMessage)> _msgQueue = new();
    private readonly SingleWaiterAutoResetEvent _signal = new();
    private SynchronizationContext _synchronizationContext;

    // 已经登录的用户
    private readonly Dictionary<uint, NetSession> _sessions = new();
    private static readonly RpcReplyAction ReplyAction = OnReply;

    public static bool EnableDebug { get; set; }

    protected override void OnAwake()
    {
        _synchronizationContext = SynchronizationContext.Current!;
        _ = Process();
    }

    protected override void OnDestroy()
    {
        foreach (var disposable in _listener)
            disposable.Dispose();
        _listener.Clear();
    }

    public void Listen(IPEndPoint bindIp)
    {
        _listener.Add(_network.Listen(bindIp.Port, bindIp.Address, this));
        Logger.Info($"listen {bindIp}");
    }

    private static bool OnReply(uint netId, IResponse rsp) => Send(netId, rsp);

    public static bool Send(uint netId, IMessage msg)
    {
        if (EnableDebug)
        {
            Logger.Debug($"send -> {msg.ToJson()}");
        }

        ByteBuf buf = ByteBuf.Rent();
        Span<byte> head = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(head, msg.MsgId);
        buf.Write(head);
        ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(buf, msg);
        return _network.Send(netId, buf);
    }

    private async Task Process()
    {
        var eventSystem = this.Root.GetComp<EventSystem>();

        while (true)
        {
            while (_msgQueue.TryDequeue(out var item))
            {
                uint netId = item.Item1;
                IMessage msg = item.Item2;
                try
                {

                    if (!_sessions.TryGetValue(netId, out var session))
                    {
                        Disconnect(netId);
                        continue;
                    }

                    //if (!session.IsLogin) // 未登录时，收到其它消息一律踢掉;
                    //{
                    //    Disconnect(netId);
                    //    continue;
                    //}

                    if (EnableDebug)
                    {
                        Logger.Debug($"recv <- {msg.ToJson()}");
                    }

                    QueueExecute(eventSystem, session, msg).Forget();
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"处理消息错误: netId={netId} msg={msg.GetType().Name}");
                    Disconnect(netId);
                }
            }
            await _signal.WaitAsync();
        }
    }

    public void Disconnect(uint netId)
    {
        if (!_sessions.Remove(netId, out var session))
            return;
        session.OnDisConnect();
        _network.Disconnect(netId);
    }

    private static async UniTask QueueExecute(EventSystem eventSystem, NetSession session, IMessage msg)
    {
        if (session.CoroutineLock != null)
        {
            using (await session.CoroutineLock.EnterAsync())
            {
                await Dispatch();
            }
        }
        else
        {
            await Dispatch();
        }

        return;

        async UniTask Dispatch()
        {
            try
            {
                if (msg is IRequest request)
                {
                    await eventSystem.Dispatch(session, request, ReplyAction, (uint)session.Id);
                }
                else
                {
                    await eventSystem.Dispatch(session, msg);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"处理客户端消息异常: {msg.GetType().Name} uid={session.UserId}");
            }
        }
    }

    private void OnSafeConnect(uint netId)
    {
        Logger.Info($"client connected: {netId}");
        var session = new NetSession(netId, this.Root);
        _sessions.Add(netId, session);
    }

    private void OnSafeDisconnect(uint netId)
    {
        Logger.Info($"client disconnect: {netId}");
        Disconnect(netId);
    }

    #region 网络线程

    public void OnConnect(uint netId)
    {
        _synchronizationContext.Post(_ => OnSafeConnect(netId), null);
    }

    public void OnConnectFailed(IPEndPoint ip, SocketError error)
    {
    }

    public void OnDisconnect(uint netId)
    {
        _synchronizationContext.Post(_ => OnSafeDisconnect(netId), null);
    }

    public void OnData(uint netId, ByteBuf buf)
    {
        Span<byte> head = stackalloc byte[4];
        _ = buf.Read(head);
        int msgId = BinaryPrimitives.ReadInt32LittleEndian(head);
        Type? msgType = MessageTypes.Ins.GetById(msgId);
        if (msgType is null)
        {
            SLog.Error($"找不到消息类型:msgId={msgId}");
            _network.Disconnect(netId);
            return;
        }

        IMessage msg = (IMessage)ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(msgType, buf);
        _msgQueue.Enqueue((netId, msg));
        _signal.Signal();
    }

    #endregion
}