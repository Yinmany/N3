using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;

namespace N3;

/// <summary>
/// 消息收件箱
/// </summary>
public class MessageInbox : AComponent, IMessageReceiver
{
    private static readonly RpcReplyAction ReplyAction = OnReply;

    public bool IsReentrant { get; set; }

    private readonly ConcurrentQueue<(ushort, IMessage)> _queue = new();
    private readonly SingleWaiterAutoResetEvent _signal = new();
    private IEventSystem _eventSystem;

    protected override void OnAwake()
    {
        _eventSystem = this.Root.GetComp<EventSystem>()!;
        ProcessAsync().Forget();
        MessageCenter.Ins.AddReceiver(this.Entity.Id, this);
    }

    protected override void OnDestroy()
    {
        MessageCenter.Ins.RemoveReceiver(this.Entity.Id);
    }

    private async UniTask ProcessAsync()
    {
        while (true)
        {
            while (_queue.TryDequeue(out var item))
            {
                ushort fromNodeId = item.Item1;
                IMessage msg = item.Item2;
                await this.Dispatch(msg, fromNodeId);
            }

            await _signal.WaitAsync();
        }
    }

    private UniTask Dispatch(IMessage msg, ushort fromNodeId)
    {
        UniTask task;
        if (msg is IRequest req)
        {
            try
            {
                task = _eventSystem.Dispatch(this.Entity, req, ReplyAction, fromNodeId);
            }
            catch (Exception e)
            {
                IResponse rsp = MessageTypes.Ins.NewResponse(req);
                rsp.ErrCode = RpcErrorCode.Exception;
                rsp.ErrMsg = e.Message;
                ReplyAction(fromNodeId, rsp);
                SLog.Error(e, "处理Req异常:");
                return UniTask.CompletedTask;
            }
        }
        else
        {
            try
            {
                task = _eventSystem.Dispatch(this.Entity, msg);
            }
            catch (Exception e)
            {
                SLog.Error(e, "处理Msg异常:");
                return UniTask.CompletedTask;
            }
        }

        if (IsReentrant)
        {
            task.Forget();
            return UniTask.CompletedTask;
        }
        else
        {
            return task;
        }
    }

    private static bool OnReply(uint netId, IResponse rsp)
    {
        // netId 实际是nodeId
        ushort nodeId = (ushort)netId;
        return MessageCenter.Ins.Send(Did.Make(0, nodeId), rsp);
    }

    #region 网络线程
    public void OnUnsafeReceive(ushort fromNodeId, IMessage message)
    {
        _queue.Enqueue((fromNodeId, message));
        _signal.Signal();
    }

    public void OnUnsafeNodeNetworkStatus(ushort nodeId, NodeNetworkState status, bool isClient)
    {

    }
    #endregion
}