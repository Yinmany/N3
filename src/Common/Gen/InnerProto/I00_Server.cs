using ProtoBuf;
using N3;
using System.Collections.Generic;
using System;

namespace ProjectX
{
    
    [ProtoContract]
    public partial class PbServerInfo 
    {
    
        [ProtoMember(1)] public uint NodeId { get; set; }
        [ProtoMember(2)] public string NodeIp { get; set; }
        [ProtoMember(3)] public uint SrvType { get; set; }
        [ProtoMember(4)] public long ActorId { get; set; }
    }
    
    [ProtoContract]
    public partial class A2W_Server_AppReq : IRequest
    {
        public const int _MsgId_ = (int)ProjectX.InnerMsgId.A2W_Server_AppReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public PbServerInfo ServerInfo { get; set; }
        /// <summary> 1=add 2=remote </summary>
        [ProtoMember(3)] public int Op { get; set; }
    }
    
    [ProtoContract]
    public partial class A2W_Server_AppRsp : IResponse
    {
        public const int _MsgId_ = (int)ProjectX.InnerMsgId.A2W_Server_AppRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
    }
    
    [ProtoContract]
    public partial class W2A_Server_AppMsg : IMessage
    {
        public const int _MsgId_ = (int)ProjectX.InnerMsgId.W2A_Server_AppMsg;
        public int MsgId => _MsgId_;
    
        [ProtoMember(1)] public PbServerInfo ServerInfo { get; set; }
        /// <summary> 1=add 2=remote </summary>
        [ProtoMember(2)] public int Op { get; set; }
    }
    
}