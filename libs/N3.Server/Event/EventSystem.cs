using Cysharp.Threading.Tasks;

namespace N3;

public interface IEventHandler
{
    void On();
}

public interface IUpdate : IEventHandler;

public interface IEventHandler<in T>
{
    void On(T arg);
}

internal interface IInvokable
{
    internal int GetTypeId();
}

public abstract class AInvokable<T> : IInvokable
{
    int IInvokable.GetTypeId() => TypeId.Get(typeof(T));
    public abstract void On(T arg);
}

public abstract class ATimer<T> : AInvokable<TimerInfo>
{
    public override void On(TimerInfo arg)
    {
        On(arg, (T)arg.State!);
    }

    protected abstract void On(TimerInfo timer, T arg);
}

public abstract class AInvokable<T, TResult> : IInvokable
{
    int IInvokable.GetTypeId() => TypeId.Get(typeof(T));
    public abstract TResult On(T arg);
}

public interface IEventSystem
{
    void Trigger<T>(int eventId, T arg);
    TResult Invoke<T, TResult>(int id, T arg);
    UniTask Dispatch(object ctx, IMessage msg);
    UniTask Dispatch(object ctx, IRequest req, RpcReplyAction reply, uint netId);
}

public sealed class EventSystem : AComponent, IEventSystem
{
    private readonly ushort _serverType;
    private readonly SynchronizationContext _synchronizationContext;
    private readonly SendOrPostCallback _unsafeTimerCallback;
    private readonly SendOrPostCallback _timerCallback;

    private EventTypes? _types;

    public EventSystem(ushort serverType)
    {
        _serverType = serverType;
        _synchronizationContext = SynchronizationContext.Current!;
        _unsafeTimerCallback = OnUnsafeTimerCallback;
        _timerCallback = OnTimerCallback;

        TypeManager.Ins.OnChanged += OnChanged;
        OnChanged();
    }

    protected override void OnDestroy()
    {
        TypeManager.Ins.OnChanged -= OnChanged;
    }

    private void OnChanged()
    {
        _synchronizationContext.Post(_ => { Interlocked.Exchange(ref _types, TypeManager.Ins.Get(_serverType)); }, null);
    }

    internal void Update()
    {
        if (_types is null)
            return;

        var updateList = _types.UpdateList;
        if (updateList is null or { Count: 0 })
            return;
        foreach (var item in updateList)
        {
            item.On();
        }
    }

    public void Trigger<T>(int eventId, T arg)
    {
        if (_types is null)
            return;

        var eventMap = _types.EventMap;
        if (!eventMap.TryGetValue(eventId, out var list))
            return;
        foreach (var obj in list)
        {
            ((IEventHandler<T>)obj).On(arg);
        }
    }

    /// <summary>
    /// 使用Type进行触发
    /// </summary>
    /// <param name="arg"></param>
    /// <typeparam name="T"></typeparam>
    public void Invoke<T>(T arg)
    {
        int id = TypeId.Cache<T>.Value;
        if (_types is null || !_types.InvokableMap2.TryGetValue(id, out var obj))
        {
            SLog.Warn($"{typeof(T)} is not invokable2: {id}");
            return;
        }

        ((AInvokable<T>)obj).On(arg);
    }

    /// <summary>
    /// 使用Type进行触发
    /// </summary>
    /// <param name="arg"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <exception cref="Exception"></exception>
    public TResult Invoke<T, TResult>(T arg)
    {
        int id = TypeId.Cache<T>.Value;
        if (_types is null || !_types.InvokableMap2.TryGetValue(id, out var obj))
            throw new Exception($"{typeof(T)} is not invokable2: {id}");
        return ((AInvokable<T, TResult>)obj).On(arg);
    }

    public void Invoke<T>(int id, T arg)
    {
        if (_types is null || !_types.InvokableMap.TryGetValue(id, out var obj))
        {
            SLog.Warn($"{typeof(T)} is not invokable: {id}");
            return;
        }

        ((AInvokable<T>)obj).On(arg);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="arg"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public TResult Invoke<T, TResult>(int id, T arg)
    {
        if (_types is null || !_types.InvokableMap.TryGetValue(id, out var obj))
            throw new Exception($"{typeof(T)} is not invokable: {id}");
        return ((AInvokable<T, TResult>)obj).On(arg);
    }

    /// <summary>
    /// 分发一个消息
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    public UniTask Dispatch(object ctx, IMessage msg)
    {
        if (_types is null || !_types.Handler.TryGetValue(msg.MsgId, out var handler))
        {
            SLog.Error($"消息处理器不存在: {msg.MsgId} {msg.GetType().FullName}");
            return UniTask.CompletedTask;
        }

        return ((IMsgHandler)handler).Invoke(ctx, msg);
    }

    /// <summary>
    /// 分发一个请求
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="req"></param>
    /// <param name="reply"></param>
    /// <param name="netId"></param>
    /// <returns></returns>
    public UniTask Dispatch(object ctx, IRequest req, RpcReplyAction reply, uint netId)
    {
        if (_types is null || !_types.Handler.TryGetValue(req.MsgId, out var handler))
        {
            SLog.Error($"消息处理器不存在: {req.MsgId} {req.GetType().FullName}");
            // IResponse rsp = MessageTypes.Ins.NewResponse(req);
            // rsp.ErrCode = RpcErrorCode.NotFoundHandler;
            // reply(netId, rsp);
            return UniTask.CompletedTask;
        }

        return ((IReqHandler)handler).Invoke(ctx, req, reply, netId);
    }

    public TimerInfo AddTimeout(TimeSpan dueTime, int type, object? ctx = null)
    {
        return TimerMgr.Ins.AddTimeout(dueTime, OnUnsafeTimerCallback, type, ctx);
    }

    public TimerInfo AddInterval(TimeSpan period, int type, object? ctx = null)
    {
        return TimerMgr.Ins.AddInterval(period, OnUnsafeTimerCallback, type, ctx);
    }

    private void OnUnsafeTimerCallback(object? state)
    {
        _synchronizationContext.Post(_timerCallback, state);
    }

    private void OnTimerCallback(object? state)
    {
        TimerInfo timer = (TimerInfo)state!;
        if (timer.IsDisposed)
            return;
        this.Invoke(timer.Type, timer);
    }
}