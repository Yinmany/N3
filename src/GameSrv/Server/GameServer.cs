using N3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectX;

public class GameServer : ServerApp
{
    public ServerDiscover Cluster => GetComp<ServerDiscover>();
    public EventSystem Event => GetComp<EventSystem>();

    public GameServer(ushort serverId, ushort serverType, string name) : base(serverId, serverType, name)
    {

    }

}
