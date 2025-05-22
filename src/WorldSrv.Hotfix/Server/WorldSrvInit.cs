using Cysharp.Threading.Tasks;
using Ystx2;
using WorldSrv;

[assembly: ServerType(ServerType.World), ServerInit(typeof(WorldSrvInit))]

namespace WorldSrv;

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