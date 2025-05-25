namespace N3;

internal class TypeManager : Singleton<TypeManager>, IAssemblyPostProcess
{
    public event Action? OnChanged;

    private readonly Dictionary<ushort, EventTypes> _map = new();

    public EventTypes? Get(ushort serverType)
    {
        return _map.GetValueOrDefault(serverType);
    }

    public void Begin()
    {
        _map.Clear();
    }

    public void Process(ushort serverType, Type type, bool isHotfix)
    {
        if (!isHotfix)
        {
            MessageTypes.Ins.Add(type);
        }

        if (!_map.TryGetValue(serverType, out var value))
        {
            value = new EventTypes();
            _map.Add(serverType, value);
        }

        value.Process(type);
    }

    public void End()
    {
        // 合并一下0的通用EventTypes
        if (_map.TryGetValue(0, out EventTypes? eventTypes))
        {
            foreach (var kv in _map)
            {
                if (kv.Key == 0)
                    continue;
                kv.Value.Add(eventTypes);
            }
        }

        foreach (var types in _map.Values)
            types.End();
        OnChanged?.Invoke();
    }
}