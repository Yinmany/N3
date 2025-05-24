using Cysharp.Threading.Tasks;

namespace N3;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MessageHandlerAttribute : Attribute
{
}

public interface IMsgHandlerBase
{
    int MsgId { get; }
}

internal interface IMsgHandler : IMsgHandlerBase
{
    UniTask Invoke(object self, IMessage msg);
}

public delegate bool RpcReplyAction(uint netId, IResponse response);

internal interface IReqHandler : IMsgHandlerBase
{
    UniTask Invoke(object self, IRequest req, RpcReplyAction reply, uint netId);
}

public abstract class MsgHandler<T, TMsg> : IMsgHandler where TMsg : IMessage
{
    public int MsgId => MessageTypes.ReflectionGetMsgId(typeof(TMsg));

    public UniTask Invoke(object self, IMessage msg) => On((T)self, (TMsg)msg);

    protected abstract UniTask On(T self, TMsg msg);
}

public abstract class ReqHandler<T, TReq, TResult> : IReqHandler where TReq : IRequest where TResult : IResponse
{
    public int MsgId => MessageTypes.ReflectionGetMsgId(typeof(TReq));

    protected readonly struct Reply(RpcReplyAction callback, int rpcId, uint netId)
    {
        public bool Send(TResult res)
        {
            res.RpcId = rpcId;
            return callback(netId, res);
        }
    }

    public UniTask Invoke(object self, IRequest req, RpcReplyAction reply, uint netId)
    {
        return On((T)self, (TReq)req, new Reply(reply, req.RpcId, netId));
    }

    protected abstract UniTask On(T self, TReq req, Reply reply);
}