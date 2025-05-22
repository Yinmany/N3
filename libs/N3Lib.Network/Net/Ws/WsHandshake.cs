using N3Lib.Buffer;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace N3Lib.Network
{
    public static class WsHandshake
    {
        public enum Protocol
        {
            None,
            Tcp,
            Ws
        }

        private const string GET = "GET";
        private const string TCP = "TCP";
        private const string KeyHeader = "Sec-WebSocket-Key";
        private const string endOfHandshake = "\r\n\r\n";
        private const int KeyLength = 24;
        private const int MergedKeyLength = 60;

        /// <summary>
        /// Guid used for WebSocket Protocol
        /// </summary>
        private const string HandshakeGUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static readonly int HandshakeGUIDLength = HandshakeGUID.Length;
        private static readonly byte[] HandshakeGUIDBytes = Encoding.ASCII.GetBytes(HandshakeGUID);

        private static readonly byte[] TcpAckBytes = new byte[] { (byte)'T', (byte)'C', (byte)'P', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

        private static bool StartWith(Span<byte> s, string prefix)
        {
            if (s.Length < prefix.Length)
                return false;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (s[i] != prefix[i])
                    return false;
            }
            return true;
        }


        public static int TryParser(ref ReadOnlySequence<byte> buffer, out Protocol protocol, out ByteBuf? ack)
        {
            ack = null;
            protocol = Protocol.None;
            if (buffer.Length < 4)
                return 0; // 数据不够

            // 判断握手请求是否完整
            Span<byte> tmp = stackalloc byte[4];
            buffer.Slice(buffer.Length - 4, 4).CopyTo(tmp);
            if (!StartWith(tmp, endOfHandshake))
                return 0; // 数据不够

            var req = buffer.Slice(0, buffer.Length);
            buffer = buffer.Slice(buffer.End); // 直接到结束位置

            //string testReq = Encoding.UTF8.GetString(req);

            /*
                GET /chat HTTP/1.1
                Host: server.example.com
                Upgrade: websocket
                Connection: Upgrade
                Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==
                Sec-WebSocket-Version: 13


             */

            req.Slice(0, 3).CopyTo(tmp);
            if (StartWith(tmp, TCP)) // 直接是tcp协议，走tcp处理即可。
            {
                protocol = Protocol.Tcp;
                ack = ByteBuf.Rent();
                CreateTcpResponse(ack);
                return 1;
            }

            if (!StartWith(tmp, GET)) // 不是get请求，直接断开连接。
                return -1;

            protocol = Protocol.Ws;
            // 找到Sec-WebSocket-Key

            // 跳过第一行
            SequencePosition? pos = req.PositionOf((byte)'\n');
            req = req.Slice(req.GetPosition(1, pos.Value));
            pos = req.PositionOf((byte)'\n');
            if (!TryHandleHandshake(req, out string? key))
                return -1;

            ack = ByteBuf.Rent();
            CreateWsResponse(key!, ack);
            return 1;
        }

        /// <summary>
        /// 处理握手请求，并返回sha1+base64后的要回的key
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool TryHandleHandshake(ReadOnlySequence<byte> buffer, out string? key)
        {
            key = null;

            //string testReq = Encoding.UTF8.GetString(buffer);

            using MemoryBlock block = PinnedBlockMemoryPool.Default.RentBlock(); // 4096
            Span<byte> span = block.Memory.Span;

            bool isFindKey = false;
            SequencePosition? pos = null;
            while ((pos = buffer.PositionOf((byte)'\n')) != null)
            {
                var line = buffer.Slice(0, pos.Value);
                buffer = buffer.Slice(buffer.GetPosition(1, pos.Value));
                //string testText111 = Encoding.UTF8.GetString(line);
                line.CopyTo(span);
                if (StartWith(span, KeyHeader))
                {
                    isFindKey = true;
                    break;
                }
            }

            if (!isFindKey)
                return false;

            //string testText123 = Encoding.UTF8.GetString(span);

            // 追加key
            Span<byte> keyBuffer = span.Slice(KeyHeader.Length + 2);

            //string testText = Encoding.UTF8.GetString(keyBuffer);
            //string testText2 = Encoding.UTF8.GetString(keyBuffer.Slice(KeyLength));
            HandshakeGUIDBytes.CopyTo(keyBuffer.Slice(KeyLength)); // 拼接key
            //string testText3 = Encoding.UTF8.GetString(keyBuffer);
            //string testText4 = Encoding.UTF8.GetString(block.Bytes, KeyHeader.Length + 2, MergedKeyLength);

            // 获取sha1 并转换 base64的key
            int len = SHA1.HashData(keyBuffer.Slice(0, MergedKeyLength), span.Slice(1000));
            key = Convert.ToBase64String(block.Bytes, 1000, len);
            return true;
        }

        private static void CreateWsResponse(string key, ByteBuf ack)
        {
            string message = string.Format(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: {0}\r\n\r\n",
                key);

            foreach (var c in message)
            {
                ack.WriteByte((byte)c);
            }
        }

        private static void CreateTcpResponse(ByteBuf ack)
        {
            ack.Write(TcpAckBytes);
        }
    }
}