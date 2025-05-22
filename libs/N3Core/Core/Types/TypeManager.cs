namespace N3Core;

internal class TypeManager : Singleton<TypeManager>, IAssemblyPostProcess
{
    public event Action? OnChanged;

    private readonly Dictionary<ushort, EventTypes> _eventTypes = new();

    public EventTypes? Get(ushort serverType)
    {
        return _eventTypes.GetValueOrDefault(serverType);
    }

    public void Begin()
    {
        _eventTypes.Clear();
    }

    public void Process(ushort serverType, Type type, bool isHotfix)
    {
        if (!isHotfix)
        {
            MessageTypes.Ins.Add(type);
        }

        if (!_eventTypes.TryGetValue(serverType, out var value))
        {
            value = new EventTypes();
            _eventTypes.Add(serverType, value);
        }

        value.Process(type);
    }

    public void End()
    {
        foreach (var types in _eventTypes.Values)
            types.End();
        OnChanged?.Invoke();
    }
}