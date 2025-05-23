using N3.Buffer;
using System.Buffers;
using System.IO.Pipelines;

namespace N3.Network;

/// <summary>
/// 混合Tcp和WebSocket实现的网络
/// </summary>
public partial class TcpAndWsNetwork : TcpNetwork, IConnHandler
{
    private readonly Handshake handshake;

    public TcpAndWsNetwork(bool useThreadPool = true, int? ioQueueCount = null) : base(useThreadPool, ioQueueCount)
    {
        handshake = new Handshake(this, new WsHandler(this));
    }

    protected override void OnConnRegister(TcpConn conn)
    {
        conn.Handler = handshake; // 添加握手协议
    }

    #region 网络IO线程

    void IConnHandler.OnConnected(TcpConn conn)
    {
        INetworkCallback cb = (INetworkCallback)conn.UserData!;
        cb.OnConnect(conn.NetId);
    }

    void IConnHandler.OnDisconnected(TcpConn conn)
    {
        base.RemoveConn(conn.NetId);
        INetworkCallback cb = (INetworkCallback)conn.UserData!;
        cb.OnDisconnect(conn.NetId);
    }

    void IConnHandler.OnRead(TcpConn conn, ref ReadOnlySequence<byte> buffer)
    {
        while (FixedLengthFieldDecoder.Default.TryParse(ref buffer, out var data))
        {
            try
            {
                INetworkCallback cb = (INetworkCallback)conn.UserData!;
                cb.OnData(conn.NetId, data);
            }
            finally
            {
                data.Release();
            }
        }
    }

    void IConnHandler.OnWrite(TcpConn conn, ByteBuf byteBuf, PipeWriter writer)
    {
        writer.WriteByFixedLengthField(byteBuf);
    }

    /// <summary>
    /// WebSocket握手处理
    /// </summary>
    private class Handshake : IConnHandler
    {
        private readonly IConnHandler tcpHandler;
        private readonly IConnHandler wsHandler;
        private WsHandshake.Protocol protocol;

        public Handshake(IConnHandler tcpHandler, IConnHandler wsHandler)
        {
            this.tcpHandler = tcpHandler;
            this.wsHandler = wsHandler;
        }

        public void OnConnected(TcpConn conn)
        {
            if (conn.IsAccept) // 客户端，发送握手请求
                return;

            //ByteBuf byteBuf = ByteBuf.Rent();
            //conn.Send(byteBuf);
        }

        public void OnDisconnected(TcpConn conn)
        {

        }

        public void OnRead(TcpConn conn, ref ReadOnlySequence<byte> buffer)
        {
            int result = WsHandshake.TryParser(ref buffer, out protocol, out var ack);
            if (result == -1) // -1=握手数据不对，断掉 1=握手成功 0=数据不够，等待下一个数据包
            {
                conn.Dispose();

            }
            else if (result == 1)
            {
                conn.Send(ack!);
            }
        }

        public void OnWrite(TcpConn conn, ByteBuf byteBuf, PipeWriter writer)
        {
            writer.WriteByteBuf(byteBuf);

            // 握手数据发送后，更换对于协议的处理程序
            if (protocol == WsHandshake.Protocol.Tcp)
            {
                conn.Handler = tcpHandler;
            }
            else if (protocol == WsHandshake.Protocol.Ws)
            {
                conn.Handler = wsHandler;
                conn.Handler.OnConnected(conn);
            }
        }
    }
    #endregion
}