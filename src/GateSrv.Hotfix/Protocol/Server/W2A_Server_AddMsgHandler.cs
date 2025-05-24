using Cysharp.Threading.Tasks;
using N3;

namespace ProjectX;

[MessageHandler]
sealed class W2A_Server_AddMsgHandler : MsgHandler<GateServer, W2A_Server_AddMsg>
{
    protected override UniTask On(GateServer self, W2A_Server_AddMsg msg)
    {
        self.Cluster.AddServerInfo(msg.ServerInfo);
        return UniTask.CompletedTask;
    }
}