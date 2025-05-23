using N3.Buffer;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace N3.Network;

public abstract class TcpNetworkBase : INetwork
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<uint, TcpConn> _conns = new();
    private readonly ConcurrentStack<uint> _idPool = new();
    private readonly SocketSchedulers _socketSchedulers;
    private uint _netIdGen = 0; // 最多65535个连接

    protected TcpNetworkBase(bool useThreadPool = true, int? ioQueueCount = null)
    {
        _socketSchedulers = new SocketSchedulers(useThreadPool, ioQueueCount);
    }

    private uint GetNetId()
    {
        if (_idPool.TryPop(out uint id))
            return id;

        if (_netIdGen == ushort.MaxValue) // 无id可用了
            return 0;
        return Interlocked.Increment(ref _netIdGen);
    }

    private void RetNetId(uint netId)
    {
        NetId id = new NetId(netId);
        ushort ver = (ushort)(id.Version + 1);
        NetId freeId = new NetId(id.Value, ver);
        _idPool.Push(freeId.Id);
    }

    public IDisposable Listen(int port, IPAddress bindAddr, INetworkCallback callback)
    {
        TcpConnListener listener = new TcpConnListener(_socketSchedulers);
        listener.Listen(port, bindAddr);
        _ = RunAsync(listener, callback, _cts.Token);
        return new ListenerHandle(listener, _cts.Token);
    }

    async Task RunAsync(TcpConnListener listener, INetworkCallback callback, CancellationToken cancellationToken)
    {
        while (true)
        {
            TcpConn? conn = await listener.AcceptAsync(cancellationToken);
            if (conn is null)
                break;

            conn.UserData = callback;
            ConnStart(conn);
        }
    }

    private void ConnStart(TcpConn conn)
    {
        uint netId = GetNetId();
        if (netId == 0)
        {
            conn.Dispose();
            return;
        }

        conn.NetId = netId;
        if (!_conns.TryAdd(netId, conn))
            throw new Exception("netId is not available");

        OnConnRegister(conn);
        conn.Start();
    }

    protected abstract void OnConnRegister(TcpConn conn);

    /// <summary>
    /// 仅仅从字典中移除，不会调用Dispose
    /// </summary>
    /// <param name="netId"></param>
    protected void RemoveConn(uint netId)
    {
        if (!_conns.TryRemove(netId, out _))
            return;
        RetNetId(netId);
    }

    public void Connect(IPEndPoint ip, INetworkCallback callback)
    {
        TcpConn conn = new TcpConn(ip, _socketSchedulers.GetScheduler());
        _ = ConnectAsync();
        return;

        async Task ConnectAsync()
        {
            try
            {
                await conn.ConnectAsync();
                conn.UserData = callback;
                ConnStart(conn);
            }
            catch (SocketException e)
            {
                callback.OnConnectFailed(ip, e.SocketErrorCode);
                conn.Dispose();
            }
        }
    }

    public void Disconnect(uint netId)
    {
        if (!_conns.Remove(netId, out var conn))
            return;
        _ = conn.CloseAsync();
    }

    public bool Send(uint netId, ByteBuf data)
    {
        if (!_conns.TryGetValue(netId, out var conn))
            return false;
        return conn.Send(data);
    }

    public IPEndPoint? GetLocalAddr(uint netId)
    {
        if (_conns.TryGetValue(netId, out var conn))
            return conn.RemoteEndPoint;
        return null;
    }

    public IPEndPoint? GetRemoteAddr(uint netId)
    {
        if (_conns.TryGetValue(netId, out var conn))
            return conn.RemoteEndPoint;
        return null;
    }
}
