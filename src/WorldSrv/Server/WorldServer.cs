using N3;

namespace ProjectX;

public class WorldServer : ServerApp
{
    public ServerDiscover Cluster => GetComp<ServerDiscover>();
    public EventSystem Event => GetComp<EventSystem>();

    public WorldServer(ushort serverId, ushort serverType, string name) : base(serverId, serverType, name)
    {

    }
}