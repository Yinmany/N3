using N3.Buffer;
using System.Net;
using System.Net.Sockets;

namespace N3.Network;

public interface INetwork
{
    IDisposable Listen(int port, IPAddress bindAddr, INetworkCallback callback);

    void Connect(IPEndPoint ip, INetworkCallback callback);

    void Disconnect(uint netId);

    bool Send(uint netId, ByteBuf data);

    IPEndPoint? GetLocalAddr(uint netId);

    IPEndPoint? GetRemoteAddr(uint netId);
    // void SetUserData(uint netId, object userData);
    // object? GetUserData(uint netId, object userData);
}

/// <summary>
/// 网络连接回调
///     同个连接的回调都在同个网络线程中
/// </summary>
public interface INetworkCallback
{
    // 连接成功
    void OnConnect(uint netId);

    // 连接失败(只用Connect会调用)
    void OnConnectFailed(IPEndPoint ip, SocketError error);

    void OnDisconnect(uint netId);

    void OnData(uint netId, ByteBuf buf);
}