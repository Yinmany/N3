using N3;

namespace ProjectX;

public class WorldServer : ServerApp
{
    public ClusterComp Cluster => GetComp<ClusterComp>();
    public EventSystem Event => GetComp<EventSystem>();

    public WorldServer(ushort serverId, ushort serverType, string name) : base(serverId, serverType, name)
    {

    }
}