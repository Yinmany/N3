using N3;
using Cysharp.Threading.Tasks;

namespace ProjectX.Protocol;

[MessageHandler]
sealed class A2W_Server_AddReqHandler : ReqHandler<WorldServer, A2W_Server_AddReq, A2W_Server_AddRsp>
{
    protected override UniTask On(WorldServer self, A2W_Server_AddReq req, Reply reply)
    {
        //this.DebugMsg(req);

        A2W_Server_AddRsp rsp = new A2W_Server_AddRsp();
        self.Cluster.AddServerInfo(req.ServerInfo);
        reply.Send(rsp);
        return UniTask.CompletedTask;
    }
}