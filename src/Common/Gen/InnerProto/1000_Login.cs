using ProtoBuf;
using N3;
using System.Collections.Generic;
using System;

namespace ProjectX
{
    
    [ProtoContract]
    public partial class L2W_LoginReq : IRequest
    {
        public const int _MsgId_ = (int)ProjectX.InnerMsgId.L2W_LoginReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int UserId { get; set; }
    }
    
    [ProtoContract]
    public partial class L2W_LoginRsp : IResponse
    {
        public const int _MsgId_ = (int)ProjectX.InnerMsgId.L2W_LoginRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
        [ProtoMember(4)] public string Token { get; set; }
        [ProtoMember(5)] public string ServerAddr { get; set; }
    }
    
}