using Cysharp.Threading.Tasks;
using N3;
using ProjectX;

[assembly: ServerType(ServerType.Gate), ServerInit(typeof(GateSrvInit))]

namespace ProjectX;

public class GateSrvInit : IServerInit
{
    public async UniTask OnInit(ServerApp app)
    {
        GateServer gate = (GateServer)app;

        app.AddComp(new MessageInbox());
        app.AddComp(new ServerDiscover());

        gate.Cluster.Init();
    }

    public UniTask OnUnInit(ServerApp app)
    {
        return UniTask.CompletedTask;
    }
}