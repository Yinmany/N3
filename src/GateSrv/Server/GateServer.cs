using N3;

namespace ProjectX;

public class GateServer : ServerApp
{
    public ClusterComp Cluster => GetComp<ClusterComp>();
    public EventSystem Event => GetComp<EventSystem>();

    public GateServer(ushort serverId, ushort serverType, string name) : base(serverId, serverType, name)
    {
    }
}
