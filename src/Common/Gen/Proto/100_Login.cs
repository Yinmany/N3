using ProtoBuf;
using N3Core;
using System.Collections.Generic;
using System;

namespace Ystx2
{
    
    [ProtoContract]
    public partial class PingReq : IRequest
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.PingReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
    }
    
    [ProtoContract]
    public partial class PingRsp : IResponse
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.PingRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
    }
    
    /// <summary> 登录请求 </summary>
    [ProtoContract]
    public partial class HttpLoginVerifyReq : IRequest
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.HttpLoginVerifyReq;
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
        public const int _MsgId_ = (int)Ystx2.MsgId.HttpLoginVerifyRsp;
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
        public const int _MsgId_ = (int)Ystx2.MsgId.HttpGetNoticeRsp;
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
        public const int _MsgId_ = (int)Ystx2.MsgId.HttpGetServerInfoReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        /// <summary> 渠道 </summary>
        [ProtoMember(2)] public string Channel { get; set; }
    }
    
    [ProtoContract]
    public partial class HttpGetServerInfoRsp : IResponse
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.HttpGetServerInfoRsp;
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
    
    /// <summary> 角色踢出消息 </summary>
    [ProtoContract]
    public partial class G2C_RoleKickoutMsg : IMessage
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.G2C_RoleKickoutMsg;
        public int MsgId => _MsgId_;
    
        [ProtoMember(1)] public int Reason { get; set; }
    }
    
    /// <summary> 登录游戏服 </summary>
    [ProtoContract]
    public partial class C2G_RoleLoginCheckReq : IRequest
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.C2G_RoleLoginCheckReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public string Token { get; set; }
    }
    
    [ProtoContract]
    public partial class C2G_RoleLoginCheckRsp : IResponse
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.C2G_RoleLoginCheckRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
    }
    
    /// <summary> 角色登录请求 </summary>
    [ProtoContract]
    public partial class C2G_RoleLoginReq : IRequest
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.C2G_RoleLoginReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
    }
    
    [ProtoContract]
    public partial class C2G_RoleLoginRsp : IResponse
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.C2G_RoleLoginRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
    }
    
    /// <summary> 进入场景请求 </summary>
    [ProtoContract]
    public partial class C2G_EnterSceneReq : IRequest
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.C2G_EnterSceneReq;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public long SceneId { get; set; }
    }
    
    [ProtoContract]
    public partial class C2G_EnterSceneRsp : IResponse
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.C2G_EnterSceneRsp;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
        /// <summary> 进入的场景id </summary>
        [ProtoMember(4)] public long SceneId { get; set; }
    }
    
}