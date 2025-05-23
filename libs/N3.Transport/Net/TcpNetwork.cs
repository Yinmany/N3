using N3.Buffer;
using System.Buffers;
using System.IO.Pipelines;

namespace N3.Network;

public class TcpNetwork : TcpNetworkBase, IConnHandler
{
    public TcpNetwork(bool useThreadPool = true, int? ioQueueCount = null) : base(useThreadPool, ioQueueCount)
    {
    }

    protected override void OnConnRegister(TcpConn conn)
    {
        conn.Handler = this;
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

    #endregion
}