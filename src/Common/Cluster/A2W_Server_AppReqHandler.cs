using N3;
using Cysharp.Threading.Tasks;

namespace ProjectX.Protocol;

[MessageHandler]
sealed class A2W_Server_AppReqHandler : ReqHandler<ServerApp, A2W_Server_AppReq, A2W_Server_AppRsp>
{
    protected override UniTask On(ServerApp self, A2W_Server_AppReq req, Reply reply)
    {
        //this.DebugMsg(req);

        A2W_Server_AppRsp rsp = new A2W_Server_AppRsp();
        var cluster = self.GetComp<ClusterComp>();
        cluster.ServerInfoChanged(req.ServerInfo, req.Op);
        reply.Send(rsp);
        return UniTask.CompletedTask;
    }
}