using N3.Buffer;
using N3.Network;
using System.Buffers.Binary;
using System.Net;

namespace N3;

public partial class MessageCenter
{
    class ClientSession : IDisposable
    {
        public readonly RpcTimeoutQueue timeoutQueue;
        private readonly ushort _nodeId;
        private readonly IOQueue _ioQueue;

        private readonly IConnHandler connHandler;
        private TcpConn _conn;
        private IPEndPoint _ip;

        public ClientSession(ushort nodeId, IPEndPoint ip, IOQueue ioQueue, IConnHandler connHandler, IReadOnlyDictionary<int, ResponseTcs> rpcCallback)
        {
            _nodeId = nodeId;
            _ip = ip;
            _ioQueue = ioQueue;
            this.connHandler = connHandler;
            timeoutQueue = new RpcTimeoutQueue(rpcCallback);
            //_ = Connect();
        }

        public void ChangeIp(IPEndPoint ip)
        {
            if (ip.ToString() == _ip.ToString())
                return;

            logger.Info($"node ip change {_nodeId} {_ip} => {ip}");
            _ip = ip;
        }

        public bool Send(ByteBuf buf)
        {
            if (_conn is null || _conn.ClosedToken.IsCancellationRequested) // 重新再次连接
                _ = Connect();
            return _conn!.Send(buf);
        }

        public void RpcCallbackDisconnectError()
        {
            timeoutQueue.Clear(RpcException.Disconnect);
        }

        private async Task Connect()
        {
            try
            {
                _conn = new TcpConn(_ip, _ioQueue);
                _conn.Handler = connHandler;
                SendNodeInfo();

                await _conn.ConnectAsync();
                logger.Info($"connect to server: dst {this._nodeId} {_ip}");
                _conn.Start();
            }
            catch (Exception e)
            {
                // 连接失败
                logger.Error(e, $"connect to server fail: {this._nodeId} {this._ip}");
                _conn.Dispose();
                RpcCallbackDisconnectError();
            }

            return;

            void SendNodeInfo()
            {
                // 发送nodeId
                ByteBuf buf = ByteBuf.Rent();
                Span<byte> head = stackalloc byte[2];
                BinaryPrimitives.WriteUInt16LittleEndian(head, Did.LocalNodeId);
                buf.Write(head);
                _conn.Send(buf);
            }
        }

        public void Dispose()
        {
            _conn.Dispose();
        }
    }
}