using N3;

namespace ProjectX;

public class GateServer : ServerApp
{
    public ServerDiscover Cluster => GetComp<ServerDiscover>();
    public EventSystem Event => GetComp<EventSystem>();

    public GateServer(ushort serverId, ushort serverType, string name) : base(serverId, serverType, name)
    {
    }
}
