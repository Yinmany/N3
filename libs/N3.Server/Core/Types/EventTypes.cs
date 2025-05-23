using System.Reflection;

namespace N3;

internal readonly struct UpdateItem(int order, IUpdate update)
{
    public int Order => order;

    public void On()
    {
        update.On();
    }
}

internal class EventTypes
{
    public List<UpdateItem> UpdateList { get; } = new();
    public Dictionary<int, List<object>> EventMap { get; } = new();
    public Dictionary<int, IInvokable> InvokableMap { get; } = new();

    /// <summary>
    /// TypeId进行触发
    /// </summary>
    public Dictionary<int, IInvokable> InvokableMap2 { get; } = new();

    public Dictionary<int, IMsgHandlerBase> Handler { get; } = new();

    public IServerInit? Init { get; private set; }

    private class UpdateItemComparer : IComparer<UpdateItem>
    {
        public int Compare(UpdateItem x, UpdateItem y)
        {
            return x.Order.CompareTo(y.Order);
        }
    }

    public void Process(Type type)
    {
        if (type.IsAssignableTo(typeof(IServerInit)))
        {
            SLog.Debug($"注册ServerApp初始化类: {type.FullName}");
            Init = (IServerInit)Activator.CreateInstance(type)!;
            return;
        }

        // ProcessUpdate(type);
        ProcessUpdateEvent(type);
        ProcessEvent(type);
        ProcessInvokable(type);
        ProcessMsgHandler(type);
    }

    // private void ProcessUpdate(Type type)
    // {
    //     EntitySystemAttribute? attr = type.GetCustomAttribute<EntitySystemAttribute>();
    //     if (attr == null) return;
    //
    //     MethodInfo? methodInfo = type.GetMethods().FirstOrDefault(f => f.GetCustomAttribute<UpdateAttribute>() != null);
    //     if (methodInfo == null) return;
    //
    //     ParameterInfo[] parameterInfos = methodInfo.GetParameters();
    //     if (parameterInfos is not { Length: 1 })
    //         throw new Exception($"{type.FullName} Update 参数错误");
    //
    //     Type paramType = parameterInfos[0].ParameterType;
    //
    //     ParameterExpression paramA = Expression.Parameter(typeof(Entity), "self");
    //     Expression converted = Expression.Convert(paramA, paramType);
    //
    //     MethodCallExpression callExpression = Expression.Call(null, methodInfo, converted);
    //     Action<Entity> updateAction = Expression.Lambda<Action<Entity>>(callExpression, paramA).Compile();
    //
    //     int typeId = TypeId.Get(paramType);
    //     _updateTmp!.Add(typeId, updateAction);
    // }

    private void ProcessUpdateEvent(Type type)
    {
        var attr = type.GetCustomAttribute<UpdateEventAttribute>();
        if (attr is null)
            return;

        IUpdate? obj = Activator.CreateInstance(type) as IUpdate;
        if (obj is null)
            return;
        UpdateList.Add(new(attr.Order, obj));
    }

    private void ProcessEvent(Type type)
    {
        var attr = type.GetCustomAttribute<EventAttribute>();
        if (attr is null)
            return;

        object? obj = Activator.CreateInstance(type);
        if (obj is null)
            return;

        if (!EventMap.TryGetValue(attr.EventId, out var list))
        {
            list = new List<object>();
            EventMap.Add(attr.EventId, list);
        }

        list.Add(obj);
    }

    private void ProcessInvokable(Type type)
    {
        var attr = type.GetCustomAttribute<InvokableAttribute>();
        if (attr is null)
            return;

        IInvokable? obj = Activator.CreateInstance(type) as IInvokable;
        if (obj is null)
            return;

        if (attr.Id == 0) // 0 自动获取TypeId
        {
            InvokableMap2[obj.GetTypeId()] = obj;
        }
        else
        {
            InvokableMap[attr.Id] = obj;
        }
    }

    private void ProcessMsgHandler(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
            return;

        var attr = type.GetCustomAttribute<MessageHandlerAttribute>();
        if (attr is null)
            return;

        IMsgHandlerBase? handler = Activator.CreateInstance(type) as IMsgHandlerBase;
        if (handler is null)
        {
            SLog.Error($"消息处理器创建失败: {type.FullName} 必须实现 {nameof(IMsgHandlerBase)} 接口");
            return;
        }

        Handler.Add(handler.MsgId, handler);
    }

    public void End()
    {
        UpdateList.Sort(new UpdateItemComparer());
    }
}