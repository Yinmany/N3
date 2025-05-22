using Cysharp.Threading.Tasks;

namespace N3.Protocol;

[MessageHandler]
sealed class C2G_RoleLoginReqHandler : ReqHandler<NetSession, C2G_RoleLoginReq, C2G_RoleLoginRsp>
{
    protected override UniTask On(NetSession self, C2G_RoleLoginReq req, Reply reply)
    {
        C2G_RoleLoginRsp rsp = new C2G_RoleLoginRsp();

        reply.Send(rsp);
        return UniTask.CompletedTask;
    }
}