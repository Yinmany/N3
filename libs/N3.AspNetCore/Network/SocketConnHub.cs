using System.Net;
using Cysharp.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.IO;
using N3;

namespace N3.AspNetCore;

public delegate ANetChannel NetChannelFactoryDelegate<T>(uint netId, T state, IPEndPoint remoteIp);

public delegate void NetChannelConnectDelegate(INetChannel channel);

public delegate void NetChannelDisconnectDelegate(INetChannel channel);

public delegate void NetChannelDataDelegate(INetChannel channel, RecyclableMemoryStream input);

/// <summary>
/// Socket连接中心(管理已经连接的Socket)
/// </summary>
public abstract class SocketConnHub<T> : Singleton<T> where T : SocketConnHub<T>
{
    private readonly Slots<ANetChannel> _connections = new(32);
    private readonly ReaderWriterLockSlim _rwLock = new();

    private readonly Action<object> _sendAllCallback;
    // public event NetChannelConnectDelegate? OnConnect;
    // public event NetChannelDisconnectDelegate? OnDisconnect;
    // public event NetChannelDataDelegate? OnDataArrived;

    protected SocketConnHub()
    {
        this._sendAllCallback = this.InternalSendAll;
    }

    /// <summary>
    /// 连接加入Hub
    /// </summary>
    /// <param name="state"></param>
    /// <param name="remoteIp"></param>
    /// <param name="newCallback"></param>
    /// <typeparam name="TState"></typeparam>
    /// <returns></returns>
    private ANetChannel? Add<TState>(TState state, IPEndPoint remoteIp, NetChannelFactoryDelegate<TState> newCallback)
    {
        _rwLock.EnterWriteLock();
        try
        {
            if (!_connections.TryAdd(null, out uint netId))
                return null;

            ANetChannel conn = newCallback(netId, state, remoteIp);
            _connections[netId] = conn;
            return conn;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 从Hub中移除连接
    /// </summary>
    /// <param name="netId"></param>
    /// <param name="isDispose"></param>
    private void Remove(uint netId, bool isDispose)
    {
        _rwLock.EnterWriteLock();
        try
        {
            if (!_connections.Remove(netId, out ANetChannel? conn))
                return;

            conn!.OnData -= OnDataArrived;
            if (isDispose)
                conn.Dispose();
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 给指定客户端发送消息(由网络接管`内存流`)
    /// </summary>
    /// <param name="netId"></param>
    /// <param name="data"></param>
    public void Send(uint netId, RecyclableMemoryStream data)
    {
        _rwLock.EnterReadLock();
        try
        {
            if (!_connections.TryGet(netId, out ANetChannel? conn))
            {
                return;
            }

            conn!.Send(data);
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    private void InternalSendAll(object state)
    {
        _rwLock.EnterReadLock();
        RecyclableMemoryStream data = (RecyclableMemoryStream)state;
        try
        {
            // 拷贝发送
            foreach (var conn in this._connections)
            {
                RecyclableMemoryStream d = NetBuffer.Rent();
                data.CopyTo(d);
                conn.Send(d);
            }
        }
        finally
        {
            _rwLock.ExitReadLock();
            data.Dispose();
        }
    }

    /// <summary>
    /// 发送给所有连接
    /// </summary>
    /// <param name="data"></param>
    public void SendAll(RecyclableMemoryStream data)
    {
        ThreadPool.QueueUserWorkItem(_sendAllCallback, data, false);
    }

    /// <summary>
    /// 断开所有连接
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task DisconnectAllAsync(CancellationToken cancellationToken)
    {
        _rwLock.EnterWriteLock();

        try
        {
            List<Task> tasks = new List<Task>();
            foreach (var conn in _connections)
            {
                tasks.Add(conn.CloseAsync(cancellationToken));
            }

            return Task.WhenAll(tasks);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    public async Task ConnectedAsync<TState>(TState state, IPEndPoint remoteIp, NetChannelFactoryDelegate<TState> factory)
    {
        ANetChannel? channel = this.Add(state, remoteIp, factory);
        if (channel is null)
        {
            SLog.Warn($"达到最大连接数, 拒绝客户端连接: ip={remoteIp}");
            return;
        }

        // ITimerNode timerNode = _timerService.AddInterval(1000 * 30, 1000 * 30, OnChannelIdleCheckTimeout, 0, channel);

        try
        {
            channel.OnData = OnDataArrived;
            OnConnect(channel);
            await channel.RunAsync();
        }
        catch (ConnectionAbortedException)
        {
        }
        finally
        {
            channel.OnData -= OnDataArrived;

            // timerNode.Dispose();
            try
            {
                OnDisconnect(channel);
            }
            finally
            {
                channel.Dispose();
                this.Remove(channel.NetId, false);
            }
        }
    }

    // private void OnChannelIdleCheckTimeout(ITimerNode timer)
    // {
    //     SLog.Debug("idle check ...");
    //     ANetChannel channel = (ANetChannel)timer.State!;
    //     if (STime.Timestamp - channel.LastReceiveTime > 30) // 超时
    //     {
    //         channel.CloseAsync().AsUniTask(false).Forget();
    //     }
    // }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="netId"></param>
    public void Disconnect(uint netId) => DisconnectAsync(netId).AsUniTask(false).Forget();

    /// <summary>
    /// 关闭连接(等待连接发送关闭消息)
    /// </summary>
    /// <param name="netId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task DisconnectAsync(uint netId, CancellationToken cancellationToken = default)
    {
        _rwLock.EnterWriteLock();
        try
        {
            if (!_connections.Remove(netId, out ANetChannel? conn))
                return Task.CompletedTask;
            conn!.OnData = null;
            return conn.CloseAsync(cancellationToken);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    protected abstract void OnConnect(INetChannel channel);
    protected abstract void OnDisconnect(INetChannel channel);
    protected abstract void OnDataArrived(INetChannel channel, RecyclableMemoryStream input);
}