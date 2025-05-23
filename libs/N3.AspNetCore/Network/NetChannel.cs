using System.Net;
using Microsoft.IO;

namespace N3.AspNetCore;

public interface INetChannel : IDisposable
{
    uint NetId { get; }

    /// <summary>
    /// 服务端的连接
    /// </summary>
    bool IsServer { get; }

    /// <summary>
    /// 远程终结点
    /// </summary>
    IPEndPoint RemoteIp { get; }

    /// <summary>
    /// 最近发送数据时间
    /// </summary>
    long LastSendTime { get; }

    /// <summary>
    /// 最近接收数据时间
    /// </summary>
    long LastReceiveTime { get; }

    /// <summary>
    /// 总共接收字节数
    /// </summary>
    long ReceiveBytes { get; }

    /// <summary>
    /// 总共发送字节数
    /// </summary>
    long SendBytes { get; }

    /// <summary>
    /// 总共接收数据包个数
    /// </summary>
    long ReceivePackets { get; }

    /// <summary>
    /// 用户数据
    /// </summary>
    object? UserData { get; set; }

    /// <summary>
    /// 发送数据(并不是马上发送)
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    bool Send(RecyclableMemoryStream data);

    /// <summary>
    /// 关闭连接(先把数据都发送后,在进行关闭)
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CloseAsync(CancellationToken cancellationToken = default);
}

public abstract class ANetChannel : INetChannel
{
    public uint NetId { get; protected init; }

    public bool IsServer { get; protected init; }

    public abstract IPEndPoint RemoteIp { get; }

    public NetChannelDataDelegate? OnData;

    /// <summary>
    /// 最近发送数据时间
    /// </summary>
    public long LastSendTime { get; protected set; }

    /// <summary>
    /// 最近接收数据时间
    /// </summary>
    public long LastReceiveTime { get; protected set; }

    /// <summary>
    /// 总共接收字节数
    /// </summary>
    public long ReceiveBytes { get; protected set; }

    /// <summary>
    /// 总共发送字节数
    /// </summary>
    public long SendBytes { get; protected set; }

    /// <summary>
    /// 总共接收数据包个数
    /// </summary>
    public long ReceivePackets { get; protected set; }

    public object? UserData { get; set; }

    /// <summary>
    /// 总共发送数据包个数
    /// </summary>
    public long SendPackets { get; protected set; }

    public abstract bool Send(RecyclableMemoryStream data);

    public abstract Task RunAsync();

    public abstract Task CloseAsync(CancellationToken cancellationToken = default);

    public abstract void Dispose();
}