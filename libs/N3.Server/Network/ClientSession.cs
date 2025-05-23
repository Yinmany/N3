using N3.Buffer;
using N3.Network;
using System.Buffers.Binary;
using System.Net;

namespace N3;

public partial class MessageCenter
{
    class ClientSession : IDisposable
    {
        private readonly Queue<(int, long)> _timeoutQueue = new();
        private readonly ushort _nodeId;
        private readonly IOQueue _ioQueue;
        private readonly Action<int, RpcException> _rpcErrCallback;
        private readonly IConnHandler connHandler;
        private TcpConn _conn;
        private IPEndPoint _ip;

        public ClientSession(ushort nodeId, IPEndPoint ip, IOQueue ioQueue, Action<int, RpcException> rpcErrCallback, IConnHandler connHandler)
        {
            _nodeId = nodeId;
            _ip = ip;
            _ioQueue = ioQueue;
            _rpcErrCallback = rpcErrCallback;
            this.connHandler = connHandler;
            //_ = Connect();
        }

        public void CheckTimeout()
        {
            while (_timeoutQueue.TryPeek(out var item))
            {
                if (item.Item2 > STime.NowMs)
                    break;
                _timeoutQueue.Dequeue();
                _rpcErrCallback(item.Item1, RpcException.Timeout);
            }
        }

        public void ChangeIp(IPEndPoint ip)
        {
            _ip = ip;
        }

        public bool Send(ByteBuf buf)
        {
            if (_conn is null || _conn.ClosedToken.IsCancellationRequested) // 重新再次连接
                _ = Connect();
            return _conn!.Send(buf);
        }

        public void AddTimeout(int rpcId, short timeout)
        {
            if (timeout < 0)
                return;
            _timeoutQueue.Enqueue((rpcId, STime.NowMs + timeout * 1000)); // 只有发送了的，才会进入超时列表
        }

        public void RpcCallbackDisconnectError()
        {
            // 清理一下rpc回调
            while (_timeoutQueue.TryDequeue(out var val))
            {
                _rpcErrCallback(val.Item1, RpcException.Disconnect);
            }
        }

        private async Task Connect()
        {
            try
            {
                _conn = new TcpConn(_ip, _ioQueue);
                _conn.Handler = connHandler;
                SendNodeInfo();

                await _conn.ConnectAsync();
                SLog.Info($"[C] node connect: dst {this._nodeId} {_ip}");
                _conn.Start();
            }
            catch (Exception e)
            {
                // 连接失败
                SLog.Error(e, $"连接Node失败: {this._ip}");
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