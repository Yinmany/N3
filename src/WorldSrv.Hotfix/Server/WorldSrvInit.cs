using Cysharp.Threading.Tasks;
using N3;

[assembly: ServerType(ServerType.World), ServerInit(typeof(WorldSrvInit))]

namespace N3;

public class WorldSrvInit : IServerInit
{
    public UniTask OnInit(ServerApp app)
    {
        SLog.Info("OnInit");
        return UniTask.CompletedTask;
    }

    public UniTask OnUnInit(ServerApp app)
    {
        SLog.Info("OnUnInit");
        return UniTask.CompletedTask;
    }
}