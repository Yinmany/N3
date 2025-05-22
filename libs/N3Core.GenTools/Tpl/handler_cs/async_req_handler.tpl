{{~ for item in usings ~}}
using {{ item }};
{{~ end ~}}

namespace {{namespace}};

[MessageHandler]
sealed class {{msg_type}}Handler : ReqHandler<{{ctx_type}}, {{msg_type}}, {{rsp_type}}>
{
    protected override UniTask On({{ctx_type}} self, {{msg_type}} req, Reply reply)
    {
        {{rsp_type}} rsp = new {{rsp_type}}();

        reply.Send(rsp);
        return UniTask.CompletedTask;
    }
}