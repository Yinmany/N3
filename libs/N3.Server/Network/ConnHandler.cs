﻿using N3.Buffer;
using N3.Network;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;

namespace N3;

public partial class MessageCenter : IConnHandler
{
    public void OnConnected(TcpConn conn)
    {
    }

    private void OnConnected2(TcpConn conn)
    {
        ushort nodeId = (ushort)conn.NetId;

        foreach (var item in _receivers.Values)
        {
            item.OnUnsafeNodeNetworkStatus(nodeId, NodeNetworkState.Connected, !conn.IsAccept);
        }
    }

    public void OnRead(TcpConn conn, ref ReadOnlySequence<byte> buffer)
    {
        while (FixedLengthFieldDecoder.Default.TryParse(ref buffer, out var data))
        {
            try
            {
                OnDataArrived(conn, data);
            }
            finally
            {
                data.Release();
            }
        }
    }

    private void OnDataArrived(TcpConn conn, ByteBuf byteBuf)
    {
        if (conn.NetId == 0)
        {
            Span<byte> head = stackalloc byte[2];
            _ = byteBuf.Read(head);
            conn.NetId = BinaryPrimitives.ReadUInt16LittleEndian(head);
            OnConnected2(conn);
            logger.Info($"connect from client: {conn.NetId} {conn.RemoteEndPoint}");
        }
        else
        {
            // 读取头
            Span<byte> head = stackalloc byte[8 + 4];
            _ = byteBuf.Read(head);
            Did tmpId = BinaryPrimitives.ReadInt64LittleEndian(head);
            int msgId = BinaryPrimitives.ReadInt32LittleEndian(head[8..]);

            // 转换为本机id
            ushort fromNodeId = tmpId.NodeId; // 消息来之节点id
            Did id = new Did(tmpId.Time, Did.LocalNodeId, tmpId.Seq);

            Type? msgType = MessageTypes.Ins.GetById(msgId);
            if (msgType is null)
            {
                logger.Error($"找不到消息类型:msgId={msgId} id={id}");
                return;
            }

            IMessage msg = (IMessage)ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(byteBuf, null, msgType);
            OnMessage(msg, id, fromNodeId);
        }
    }

    private void OnMessage(IMessage msg, Did id, ushort fromNodeId)
    {
        if (msg is IResponse resp)
        {
            _workQueue.Post(_rspCallback, resp);
            return;
        }

        if (!_receivers.TryGetValue(id, out var receiver))
        {
            logger.Error($"找不到消息接收者:msgId={msg.MsgId} id={id}");
            if (msg is IRequest req)
            {
                IResponse rsp = MessageTypes.Ins.NewResponse(req);
                rsp.ErrCode = RpcErrorCode.NotFoundTarget;
                rsp.ErrMsg = $"找不到消息接收者: {id}";
                Ins.Send(Did.Make(0, fromNodeId), rsp); // 回给发送方
            }

            return;
        }

        receiver.OnUnsafeReceive(fromNodeId, msg);
    }



    public void OnDisconnected(TcpConn conn)
    {
        if (conn.NetId == 0)
            return;

        _workQueue.Post(_ =>
        {
            ushort nodeId = (ushort)conn.NetId;
            if (!_sessions.TryGetValue(nodeId, out var session))
                return;
            session.RpcCallbackDisconnectError();

            foreach (var item in _receivers.Values)
            {
                item.OnUnsafeNodeNetworkStatus(nodeId, NodeNetworkState.Disconnected, !conn.IsAccept);
            }
        }, null);
    }

    private void OnResponse(object? state)
    {
        IResponse rsp = (IResponse)state!;
        if (!_callbacks.Remove(rsp.RpcId, out var tcs))
            return;
        IRequest req = tcs.Request;
        ushort nodeId = tcs.ReqNodeId;
        long sendTime = tcs.SendTime;

        if (rsp.ErrCode < 0)
        {
            tcs.SetException(new RpcException(rsp.ErrCode, rsp.ErrMsg));
            return;
        }

        tcs.SetResult(rsp);

        long ms = STime.NowMs - sendTime; // rpc请求耗时
        if (ms > 300)
        {
            logger.Warn($"rpc耗时 > 300ms: {req.GetType().Name} -> {rsp.GetType().Name} {ms}ms nodeId={nodeId}");
        }
    }

    public void OnWrite(TcpConn conn, ByteBuf byteBuf, PipeWriter writer)
    {
        writer.WriteByFixedLengthField(byteBuf);
    }
}