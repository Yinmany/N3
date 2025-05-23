using ProtoBuf;
using N3;
using System.Collections.Generic;
using System;

namespace ProjectX
{
    
    [ProtoContract]
    public partial class PingReq : IRequest
    {
        public const int _MsgId_ = (int)ProjectX.MsgId.PingReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
    }
    
    [ProtoContract]
    public partial class PingRsp : IResponse
    {
        public const int _MsgId_ = (int)ProjectX.MsgId.PingRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
    }
    
    /// <summary> 登录请求 </summary>
    [ProtoContract]
    public partial class HttpLoginVerifyReq : IRequest
    {
        public const int _MsgId_ = (int)ProjectX.MsgId.HttpLoginVerifyReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        /// <summary> 账号 </summary>
        [ProtoMember(2)] public string Account { get; set; }
        /// <summary> 密码 </summary>
        [ProtoMember(3)] public string Pwd { get; set; }
    }
    
    [ProtoContract]
    public partial class HttpLoginVerifyRsp : IResponse
    {
        public const int _MsgId_ = (int)ProjectX.MsgId.HttpLoginVerifyRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
        [ProtoMember(4)] public string Token { get; set; }
        [ProtoMember(5)] public string ServerAddr { get; set; }
    }
    
    /// <summary> 获取公告信息 </summary>
    [ProtoContract]
    public partial class HttpGetNoticeRsp : IResponse
    {
        public const int _MsgId_ = (int)ProjectX.MsgId.HttpGetNoticeRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
        [ProtoMember(4)] public string Title { get; set; }
        [ProtoMember(5)] public string Context { get; set; }
        /// <summary> 是否存在公告 </summary>
        [ProtoMember(6)] public bool IsExists { get; set; }
    }
    
    [ProtoContract]
    public partial class HttpGetServerInfoReq : IRequest
    {
        public const int _MsgId_ = (int)ProjectX.MsgId.HttpGetServerInfoReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        /// <summary> 渠道 </summary>
        [ProtoMember(2)] public string Channel { get; set; }
    }
    
    [ProtoContract]
    public partial class HttpGetServerInfoRsp : IResponse
    {
        public const int _MsgId_ = (int)ProjectX.MsgId.HttpGetServerInfoRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
        /// <summary> 资源更新地址 </summary>
        [ProtoMember(4)] public string ResAddr { get; set; }
        /// <summary> 后备地址 </summary>
        [ProtoMember(5)] public string ResAddr2 { get; set; }
        /// <summary> 登录服地址 </summary>
        [ProtoMember(6)] public string LoginServer { get; set; }
    }
    
    [ProtoContract]
    public partial class C2G_Login_GateReq : IRequest
    {
        public const int _MsgId_ = (int)ProjectX.MsgId.C2G_Login_GateReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
    }
    
    [ProtoContract]
    public partial class C2G_Login_GateRsp : IResponse
    {
        public const int _MsgId_ = (int)ProjectX.MsgId.C2G_Login_GateRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
    }
    
}