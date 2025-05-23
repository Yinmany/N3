using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;

namespace N3;

/// <summary>
/// Actor组件，用于接受处理消息
/// </summary>
public class ActorComp : AComponent, IMessageReceiver
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
                        continue;
                    }
                }
                else
                {
                    task = _eventSystem.Dispatch(this.Entity, msg);
                }

                if (IsReentrant)
                {
                    task.Forget();
                }
                else
                {
                    await task;
                }
            }

            await _signal.WaitAsync();
        }
    }

    private static bool OnReply(uint netId, IResponse rsp)
    {
        return true;
    }

    public void OnUnsafeReceive(ushort fromNodeId, IMessage message)
    {
        _queue.Enqueue((fromNodeId, message));
        _signal.Signal();
    }
}