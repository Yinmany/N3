{{~ for item in usings ~}}
using {{ item }};
{{~ end ~}}

namespace {{namespace}};

[MessageHandler]
sealed class {{msg_type}}Handler : MsgHandler<{{ctx_type}}, {{msg_type}}>
{
    protected override UniTask On({{ctx_type}} self, {{msg_type}} msg)
    {
        return UniTask.CompletedTask;
    }
}