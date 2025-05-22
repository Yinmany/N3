using Cysharp.Threading.Tasks;

namespace N3.Protocol;

[MessageHandler]
sealed class C2G_EnterSceneReqHandler : ReqHandler<NetSession, C2G_EnterSceneReq, C2G_EnterSceneRsp>
{
    protected override UniTask On(NetSession self, C2G_EnterSceneReq req, Reply reply)
    {
        C2G_EnterSceneRsp rsp = new C2G_EnterSceneRsp();

        reply.Send(rsp);
        return UniTask.CompletedTask;
    }
}