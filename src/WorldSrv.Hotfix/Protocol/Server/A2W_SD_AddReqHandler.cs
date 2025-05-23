using N3;
using Cysharp.Threading.Tasks;
using MongoDB.Bson;

namespace ProjectX.Protocol;

[MessageHandler]
sealed class A2W_SD_AddReqHandler : ReqHandler<ServerApp, A2W_SD_AddReq, A2W_SD_AddRsp>
{
    protected override UniTask On(ServerApp self, A2W_SD_AddReq req, Reply reply)
    {
        SLog.Info(req.ToJson());

        A2W_SD_AddRsp rsp = new A2W_SD_AddRsp();

        reply.Send(rsp);
        return UniTask.CompletedTask;
    }
}