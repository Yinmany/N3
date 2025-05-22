{{
    is_msg = true
    is_req = false
    is_resp = false
    startOrder = 0
    if string.ends_with name 'Msg'
        _t = ': IMessage'
    else if string.ends_with name 'Req'
        _t = ': IRequest'
        is_req = true
    else if string.ends_with name 'Resp'
        _t = ': IResponse'
        is_resp = true
    else 
        is_msg = false
    end
}}
[MemoryPackable]
public partial class {{ name }} {{_t}}
{
    {{~ if is_msg ~}}
    public const ushort Opcode = {{ opcode }};
    ushort IMessageObject.GetOpcode() => Opcode;
    {{~ end ~}}
    
    {{~ if is_req || is_resp ~}}
    [MemoryPackOrder(0)] public uint RpcId { get; set; }
    {{~ end ~}}
    
{{~ for e in elems ~}}
    {{- if e.type == "ProtoMessageField" ~}}
        {{~ if !(string.empty e.tail_comment)~}}
    /// <summary>
    /// {{ e.tail_comment }}
    /// </summary>
        {{~ end ~}}
        {{~ if e.is_repeated ~}}
    [MemoryPackOrder({{e.index + startOrder}})] public List<{{e.field_type}}> {{e.name}} { get; set; } 
        {{~ else ~}}
    [MemoryPackOrder({{e.index + startOrder}})] public {{e.field_type}} {{e.name}} { get; set; }
        {{~ end ~}}
    {{~ else if e.type == "ProtoComment" ~}}
    {{ e.comment }}
    {{~ end ~}}
{{~ end ~}}
    {{ enum_codes ~}}
    {{ msg_codes }}
}