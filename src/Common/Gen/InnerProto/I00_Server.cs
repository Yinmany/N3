using ProtoBuf;
using N3;
using System.Collections.Generic;
using System;

namespace ProjectX
{
    
    [ProtoContract]
    public partial class A2W_SD_AddReq : IRequest
    {
        public const int _MsgId_ = (int)ProjectX.InnerMsgId.A2W_SD_AddReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public uint NodeId { get; set; }
        [ProtoMember(3)] public string NodeIp { get; set; }
        [ProtoMember(4)] public uint SrvType { get; set; }
    }
    
    [ProtoContract]
    public partial class A2W_SD_AddRsp : IResponse
    {
        public const int _MsgId_ = (int)ProjectX.InnerMsgId.A2W_SD_AddRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
    }
    
    [ProtoContract]
    public partial class W2A_SD_AddMsg : IMessage
    {
        public const int _MsgId_ = (int)ProjectX.InnerMsgId.W2A_SD_AddMsg;
        public int MsgId => _MsgId_;
    
        [ProtoMember(1)] public uint NodeId { get; set; }
        [ProtoMember(2)] public string NodeIp { get; set; }
        [ProtoMember(3)] public uint SrvType { get; set; }
    }
    
}