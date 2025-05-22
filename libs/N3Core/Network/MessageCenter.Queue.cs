using System.Buffers.Binary;

namespace N3Core;

public partial class MessageCenter
{
    public void Execute()
    {
        while (true)
        {
            this.Process();
            ProcessSend();
            _doWorking = 0;
            Thread.MemoryBarrier();
            if (this.IsEmpty && _sendQueue.IsEmpty)
                break;
            if (Interlocked.Exchange(ref _doWorking, 1) == 1)
                break;
        }
    }

    private void TryExecute()
    {
        if (Interlocked.CompareExchange(ref _doWorking, 1, 0) == 0)
        {
            ThreadPool.UnsafeQueueUserWorkItem(this, false);
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
        ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(buf, msg);
        return buf;
    }

    private void ProcessSend()
    {
        while (_sendQueue.TryDequeue(out var item))
        {
            Did tmpId = item.Item1;
            Did id = new Did(tmpId.Time, Did.LocalNodeId, tmpId.Seq); // 反转一下nodeId
            object sendItem = item.Item2;

            ClientSession? session = GetSession(id.NodeId);
            if (sendItem is IMessage msg)
            {
                if (session is null)
                    continue;
                ByteBuf byteBuf = Serialize(id, msg);
                session.Send(byteBuf);
            }
            else
            {
                ResponseTcs tcs = (ResponseTcs)sendItem;
                if (session is null)
                {
                    tcs.SetException(RpcException.NotFoundNode);
                    continue;
                }

                OnSendReq(id, session, tcs);
            }
        }

        return;

        void OnSendReq(in Did id, ClientSession session, ResponseTcs tcs)
        {
            IRequest req = tcs.Request;
            int rpcId = ++_rpcIdGen;
            req.RpcId = rpcId;

            ByteBuf byteBuf = Serialize(id, req);
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

            session.AddTimeout(rpcId, tcs.Timeout);
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