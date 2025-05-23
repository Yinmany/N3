using N3.Buffer;
using System.Buffers;
using System.IO.Pipelines;

namespace N3.Network;

public interface IConnHandler
{
    void OnConnected(TcpConn conn);


    void OnRead(TcpConn conn, ref ReadOnlySequence<byte> buffer);

    /// <summary>
    /// 调用后，会自动释放一次ByteBuf的引用计数
    /// </summary>
    /// <param name="conn"></param>
    /// <param name="byteBuf"></param>
    /// <param name="writer"></param>
    void OnWrite(TcpConn conn, ByteBuf byteBuf, PipeWriter writer);
    void OnDisconnected(TcpConn conn);
}