namespace N3;

public class NetSession(uint netId, Entity root) : Entity(netId, root)
{
    /// <summary>
    /// 是否登录
    /// </summary>
    public bool IsLogin { get; set; }

    public CoroutineLock? CoroutineLock { get; set; }

    public int RoleId { get; set; }

    /// <summary>
    /// 是否断开连接
    /// </summary>
    public bool IsDisConnect { get; private set; }

    /// <summary>
    /// 销毁session(会进行连接断开调用)
    /// </summary>
    public void Dispose()
    {
        Destroy();
    }

    internal void OnDisConnect()
    {
        this.Root.GetComp<EventSystem>().Invoke(InvokeId.NetDisConnect, this);
        this.IsDisConnect = true;
    }

    protected override void OnDestroy()
    {
        // 断掉连接
        NetworkComp comp = this.Root.GetComp<NetworkComp>();
        comp.Disconnect((uint)this.Id);
    }

    /// <summary>
    /// 发送消息到客户端
    /// </summary>
    /// <param name="msg"></param>
    public void Send(IMessage msg)
    {
        NetworkComp.Send((uint)this.Id, msg);
    }
}