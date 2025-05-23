namespace N3;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public class ServerTypeAttribute(ushort type) : Attribute
{
    public ushort ServerType { get; } = type;
}

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public class ServerInitAttribute(Type initType) : Attribute
{
    public Type? InitType { get; } = initType;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class EntitySystemAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class UpdateAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class TimerAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class EventAttribute(int eventId) : Attribute
{
    public int EventId { get; } = eventId;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class UpdateEventAttribute(int order = 0) : Attribute
{
    public int Order { get; } = order;
}

/// <summary>
/// id = 为0时，使用TypeId，触发时将不传Id参数
/// </summary>
/// <param name="id"></param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class InvokableAttribute(int id) : Attribute
{
    public int Id { get; } = id;
}