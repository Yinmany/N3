using N3.Buffer;
using System.Buffers.Binary;

namespace N3;

public partial class MessageCenter
{
    private sealed class InternalWorkQueue : WorkQueue, IThreadPoolWorkItem
    {
        private int _doWorking = 0;
        private readonly MessageCenter messageCenter;

        public InternalWorkQueue(MessageCenter messageCenter)
        {
            this.messageCenter = messageCenter;
        }

        void IThreadPoolWorkItem.Execute()
        {
            while (true)
            {
                this.Process();
                messageCenter.ProcessSend();
                _doWorking = 0;
                Thread.MemoryBarrier();
                if (this.IsEmpty && messageCenter._sendQueue.IsEmpty)
                    break;
                if (Interlocked.Exchange(ref _doWorking, 1) == 1)
                    break;
            }
        }

        public void TryExecute()
        {
            if (Interlocked.CompareExchange(ref _doWorking, 1, 0) == 0)
            {
                ThreadPool.UnsafeQueueUserWorkItem(this, false);
            }
        }

        protected override bool TryInlineExecute(SendOrPostCallback d, object? state)
        {
            return false;
        }

        protected override void OnPostWorkItem()
        {
            TryExecute();
        }

        protected override void OnUpdate()
        {
        }
    }

    // 序列化消息
    private ByteBuf Serialize(Did id, IMessage msg)
    {
        // 反转一下nodeId
        Did dstId = new Did(id.Time, Did.LocalNodeId, id.Seq);
        ByteBuf buf = ByteBuf.Rent();

        Span<byte> head = stackalloc byte[8 + 4];
        BinaryPrimitives.WriteInt64LittleEndian(head, dstId);
        BinaryPrimitives.WriteInt32LittleEndian(head[8..], msg.MsgId);
        buf.Write(head);

        ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(buf, msg);
        return buf;
    }

    private void ProcessSend()
    {
        while (_sendQueue.TryDequeue(out var item))
        {
            Did dstId = item.Item1;
            object sendItem = item.Item2;

            // 进程内发送
            if (dstId.NodeId == Did.LocalNodeId)
            {
                InnerSend(dstId, sendItem);
            }
            else
            {
                Did id = new Did(dstId.Time, Did.LocalNodeId, dstId.Seq); // 反转一下nodeId
                OuterSend(dstId.NodeId, id, sendItem);
            }
        }
    }

    private void InnerSend(Did dstId, object sendItem)
    {
        if (sendItem is IMessage msg)
        {
            this.OnMessage(msg, dstId, Did.LocalNodeId);
        }
        else
        {
            ResponseTcs tcs = (ResponseTcs)sendItem;
            IRequest req = tcs.Request;
            int rpcId = ++_rpcIdGen;
            req.RpcId = rpcId;

            if (!_callbacks.TryAdd(rpcId, tcs))
            {
                tcs.SetException(RpcException.DuplicateRpcId);
                return;
            }

            _innerTimeoutQueue.Enqueue(rpcId, tcs.Timeout);
            this.OnMessage(req, dstId, Did.LocalNodeId);
        }
    }

    private void OuterSend(ushort dstNodeId, Did dstId, object sendItem)
    {
        ClientSession? session = GetSession(dstNodeId);
        if (sendItem is IMessage msg)
        {
            if (session is null)
                return;
            ByteBuf byteBuf = Serialize(dstId, msg);
            session.Send(byteBuf);
        }
        else
        {
            ResponseTcs tcs = (ResponseTcs)sendItem;
            if (session is null)
            {
                tcs.SetException(RpcException.NotFoundNode);
                return;
            }

            IRequest req = tcs.Request;
            int rpcId = ++_rpcIdGen;
            req.RpcId = rpcId;

            ByteBuf byteBuf = Serialize(dstId, req);
            if (!session.Send(byteBuf))
            {
                tcs.SetException(RpcException.Disconnect);
                return;
            }

            if (!_callbacks.TryAdd(rpcId, tcs))
            {
                tcs.SetException(RpcException.DuplicateRpcId);
                return;
            }

            session.timeoutQueue.Enqueue(rpcId, tcs.Timeout);
        }
    }
}