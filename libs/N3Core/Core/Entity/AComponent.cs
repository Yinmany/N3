namespace N3Core;

public abstract class AComponent
{
    private Entity? _entity;

    /// <summary>
    /// 版本号(实体销毁时+1)
    /// </summary>
    public uint Version { get; private set; } = 1;

    public Entity Root => Entity.Root;

    public T RootAs<T>() where T : Entity
    {
        object o = Root;
        return (T)o;
    }

    public Entity Entity
    {
        get => _entity!;
        internal set
        {
            _entity = value;
            OnAwake();
        }
    }

    public T EntityAs<T>() where T : Entity => (T)Entity;

    // 用于移除所有时的link(防止迭代时访问集合)
    internal AComponent? _tail = null;

    internal void Destroy()
    {
        OnDestroy();
        _tail = null;
        ++this.Version;
        _entity = null;
    }

    protected virtual void OnAwake()
    {
    }

    protected virtual void OnDestroy()
    {
    }
}