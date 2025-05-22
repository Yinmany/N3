using N3Lib.Buffer;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;

namespace N3Lib.Network;

public partial class TcpAndWsNetwork
{
    private ref struct WebSocketFrame
    {
        public readonly WebSocketOpcode Opcode;
        public readonly ByteBuf Data;

        public WebSocketFrame(WebSocketOpcode opcode, ByteBuf data)
        {
            Opcode = opcode;
            Data = data;
        }
    }

    private enum WebSocketOpcode : byte
    {
        Continuation = 0,
        Text = 1,
        Binary = 2,
        Close = 8,
        Ping = 9,
        Pong = 10
    }

    private class WsHandler : IConnHandler
    {
        private readonly TcpAndWsNetwork network;

        public WsHandler(TcpAndWsNetwork network)
        {
            this.network = network;
        }

        public void OnConnected(TcpConn conn)
        {
            INetworkCallback cb = (INetworkCallback)conn.UserData!;
            cb.OnConnect(conn.NetId);
        }

        public void OnDisconnected(TcpConn conn)
        {
            network.RemoveConn(conn.NetId);
            INetworkCallback cb = (INetworkCallback)conn.UserData!;
            cb.OnDisconnect(conn.NetId);
        }

        public void OnRead(TcpConn conn, ref ReadOnlySequence<byte> buffer)
        {
            while (TryDecode(conn, ref buffer, out WebSocketFrame frame))
            {
                try
                {
                    if (frame.Opcode == WebSocketOpcode.Close)
                    {
                        conn.Dispose();
                        break;
                    }
                    else if (frame.Opcode == WebSocketOpcode.Ping)
                    {
                        // 发回去
                        Encode(WebSocketOpcode.Pong, frame.Data, conn.Output);
                        return;
                    }
                    else if (frame.Opcode == WebSocketOpcode.Pong)
                    {
                        continue;
                    }

                    if (frame.Data is null)
                    {
                        conn.Dispose();
                        return;
                    }

                    INetworkCallback cb = (INetworkCallback)conn.UserData!;
                    cb.OnData(conn.NetId, frame.Data);
                }
                finally
                {
                    frame.Data?.Release();
                }
            }
        }

        public void OnWrite(TcpConn conn, ByteBuf byteBuf, PipeWriter writer)
        {
            Encode(WebSocketOpcode.Binary, byteBuf, writer);
        }

        private static bool TryDecode(TcpConn conn, ref ReadOnlySequence<byte> buffer, out WebSocketFrame frame)
        {
            frame = default;
            // |FIN、RSV1、RSV2、RSV3、Opcode| Mask、PayloadLength、MaskingKey
            if (buffer.Length < 2)
                return false;

            Span<byte> headTmp = stackalloc byte[2];
            buffer.Slice(0, 2).CopyTo(headTmp);

            bool fin = (headTmp[0] & 0x80) != 0; // 1=结束，0=分片 // ...忽略 rsv1-3

            byte opcode = (byte)(headTmp[0] & 0x0f); // 1=文本，2=二进制，8=关闭，9=ping，10=pong

            bool mask = (headTmp[1] & 0x80) != 0;
            long payloadLen = (headTmp[1] & 0x7f);

            var tmpBuffer = buffer.Slice(2);
            byte payloadLengthSize = 0;
            if (payloadLen == 126)
            {
                if (buffer.Length < 4) // 后2字节
                    return false;

                payloadLen = buffer.Slice(2, 2).ReadInt16();
                tmpBuffer = buffer.Slice(4);
                payloadLengthSize = 2;
            }
            else if (payloadLen == 127)
            {
                if (buffer.Length < 10) // 后8字节
                    return false;
                payloadLen = buffer.Slice(2, 8).ReadInt64();
                tmpBuffer = buffer.Slice(10);
                payloadLengthSize = 8;
            }

            // 整个协议包的长度
            long pkgSize = 2 + payloadLengthSize + (mask ? 4 : 0) + payloadLen;
            if (buffer.Length < pkgSize) // 数据还不够
                return false;

            Span<byte> maskKey = stackalloc byte[4];
            if (mask && payloadLen > 0)
            {
                tmpBuffer.Slice(0, 4).CopyTo(maskKey);
            }

            long palyloadOffset = pkgSize - payloadLen;
            var payload = buffer.Slice(palyloadOffset, payloadLen);
            buffer = buffer.Slice(palyloadOffset + payloadLen); // 消耗掉一个完整的数据
            if (payloadLen == 0) // 无数据
            {
                frame = new WebSocketFrame((WebSocketOpcode)opcode, null);
                return true;
            }

            ByteBuf byteBuf = conn.UserData2 as ByteBuf ?? ByteBuf.Rent();

            if (mask)
            {
                payload.CopyTo(byteBuf, maskKey);
            }
            else
            {
                payload.CopyTo(byteBuf);
            }

            if (!fin)
            {
                conn.UserData2 = byteBuf;
                return false; // 数据不够，等待后面数据。
            }
            frame = new WebSocketFrame((WebSocketOpcode)opcode, byteBuf);
            conn.UserData2 = null;
            return true;
        }

        private static void Encode(WebSocketOpcode opcode, ByteBuf data, PipeWriter writer)
        {
            Span<byte> headTmp = stackalloc byte[10];
            headTmp[0] = (byte)(0x80 | (byte)opcode); // fin + opcode

            if (data == null)
            {
                headTmp[1] = 0;
                headTmp = headTmp[..2];
            }
            else if (data.Length < 126) // mask + payloadLength
            {
                headTmp[1] = (byte)data.Length;
                headTmp = headTmp[..2];
            }
            else if (data.Length < 65536)
            {
                headTmp[1] = 126;
                BinaryPrimitives.WriteUInt16LittleEndian(headTmp[2..], (ushort)data.Length);
                headTmp = headTmp[..4];
            }
            else
            {
                headTmp[1] = 127;
                BinaryPrimitives.WriteUInt64LittleEndian(headTmp[2..], (ulong)data.Length);
                //headTmp = headTmp[..10];
            }

            // 写入头部
            writer.Write(headTmp);
            if (data != null)
            {
                writer.WriteByteBuf(data);
            }
        }
    }
}