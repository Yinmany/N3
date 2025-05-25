using Cysharp.Threading.Tasks;
using N3;

namespace ProjectX;

[MessageHandler]
sealed class W2A_Server_AppMsgHandler : MsgHandler<ServerApp, W2A_Server_AppMsg>
{
    protected override UniTask On(ServerApp self, W2A_Server_AppMsg msg)
    {
        ClusterComp cluster = self.GetComp<ClusterComp>();
        cluster.ServerInfoChanged(msg.ServerInfo, msg.Op);
        return UniTask.CompletedTask;
    }
}