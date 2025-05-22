using Cysharp.Threading.Tasks;

namespace N3.Protocol;

[MessageHandler]
sealed class C2G_RoleLoginCheckReqHandler : ReqHandler<NetSession, C2G_RoleLoginCheckReq, C2G_RoleLoginCheckRsp>
{
    protected override UniTask On(NetSession self, C2G_RoleLoginCheckReq req, Reply reply)
    {
        C2G_RoleLoginCheckRsp rsp = new C2G_RoleLoginCheckRsp();

        reply.Send(rsp);
        return UniTask.CompletedTask;
    }
}