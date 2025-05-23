namespace N3;

public abstract class Entity
{
    public long Id { get; private set; }

    /// <summary>
    /// 版本号(实体销毁时+1)
    /// </summary>
    public uint Version { get; private set; } = 1;

    private bool _isDestroyFlag = false;

    private Entity? _root = null;
    private Entity? _parent = null;
    private Dictionary<long, Entity>? _children;
    private Dictionary<int, AComponent>? _childrenByTypeId;

    // 用于移除所有时的link(防止迭代时访问集合)
    private Entity? _tail = null;

    /// <summary>
    /// 获取Root(实现IEntityRoot的实体，此值就是自己)
    /// </summary>
    /// <exception cref="Exception">没有Root</exception>
    public Entity Root
    {
        get
        {
            if (_root != null)
                return _root;
            return this;
        }
    }

    public T RootAs<T>() where T : Entity
    {
        object o = Root;
        return (T)o;
    }

    /// <summary>
    /// 获取Parent
    /// </summary>
    /// <exception cref="Exception">没有Parent</exception>
    public Entity Parent => _parent ?? throw new Exception("当前实体没有Parent");

    public T ParentAs<T>() where T : Entity => (T)Parent;

    protected Entity()
    {
    }

    protected Entity(long id)
    {
        this.Id = id;
    }

    protected Entity(long id, Entity root)
    {
        if (_root is not null)
            throw new Exception("指定的Root实体，并非Root");
        this.Id = id;
        this._root = root;
    }

    public void AddChild(Entity entity)
    {
        if (entity == this)
            throw new Exception("不能添加自己.");

        // 先调用一次,一定得有root才能进行Add操作
        var checkRoot = this.Root;

        if (entity._parent != null)
            throw new Exception($"实体{entity.Id}已经Add到其他实体中");

        _children ??= new Dictionary<long, Entity>();
        _children.Add(entity.Id, entity);
        entity._parent = this;
        entity._root = checkRoot;
    }

    public Entity? GetChild(long id)
    {
        Entity? result = null;
        _children?.TryGetValue(id, out result);
        return result;
    }

    public T? GetChild<T>(long id) where T : Entity
    {
        Entity? result = null;
        _children?.TryGetValue(id, out result);
        return result as T;
    }

    public bool RemoveChild(long id)
    {
        if (_children != null && _children.Remove(id, out var entity))
        {
            entity.InternalDestroy(false);
            return true;
        }

        return false;
    }

    public void RemoveAllChild()
    {
        Entity? tail = null; // 使用链表形式，把所有要移除的entity连接起来
        Entity? head = null;
        if (_children != null)
        {
            foreach (var e in _children.Values)
            {
                if (tail is null)
                    head = tail = e;
                else
                    tail = tail._tail = e;
            }

            _children.Clear();
            _children = null;
        }

        while (head != null)
        {
            var cur = head;
            head = cur._tail;
            cur.InternalDestroy(false);
        }
    }

    public T AddComp<T>(T comp) where T : AComponent
    {
        // 先调用一次,一定得有root才能进行Add操作
        var checkRoot = this.Root;
        if (comp.Entity != null)
            throw new Exception($"组件已经挂载: {comp.Entity.GetType().Name}");

        _childrenByTypeId ??= new Dictionary<int, AComponent>();
        _childrenByTypeId.Add(TypeId.Cache<T>.Value, comp);
        comp.Entity = this;
        return comp;
    }

    public T GetComp<T>() where T : AComponent
    {
        if (_childrenByTypeId is null)
            throw new Exception($"组件没有挂载: {typeof(T).Name}");
        if (!_childrenByTypeId.TryGetValue(TypeId.Cache<T>.Value, out var result))
            throw new Exception($"组件没有挂载: {typeof(T).Name}");
        return (T)result;
    }

    public bool RemoveComp<T>() where T : AComponent
    {
        if (_childrenByTypeId != null && _childrenByTypeId.Remove(TypeId.Cache<T>.Value, out var comp))
        {
            comp.Destroy();
            return true;
        }

        return false;
    }

    public void RemoveAllComp()
    {
        if (_childrenByTypeId == null || _childrenByTypeId.Count == 0)
            return;

        AComponent? tail = null;
        AComponent? head = null;
        foreach (var e in _childrenByTypeId.Values)
        {
            if (tail is null)
                head = tail = e;
            else
                tail = tail._tail = e;
        }

        _childrenByTypeId.Clear();
        _childrenByTypeId = null;

        while (head != null)
        {
            var cur = head;
            head = cur._tail;
            cur.Destroy();
        }
    }

    protected void Destroy() => InternalDestroy(true);

    private void InternalDestroy(bool isRemove)
    {
        // 防止递归调用(在移除子实体或组时，还调用了父级的销毁)
        if (_isDestroyFlag)
            return;
        _isDestroyFlag = true;

        OnDestroy();

        this.RemoveAllChild();
        this.RemoveAllComp();

        // 从parent中移除掉
        if (isRemove && this._parent is { _children: not null })
        {
            this._parent._children.Remove(this.Id);
        }

        this._parent = null;
        this._root = null;
        _isDestroyFlag = true;
        ++this.Version;
    }

    protected virtual void OnDestroy()
    {
    }
}