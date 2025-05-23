using ProtoBuf;
using N3;
using System.Collections.Generic;

namespace ProjectX
{
    public enum MsgId
    {
        None = 0,
        PingReq = 1,
        PingRsp = 2,
        /// <summary>
        /// http登录使用的
        /// </summary>
        HttpLoginVerifyReq = 1001,
        HttpLoginVerifyRsp = 1002,
        HttpGetNoticeRsp = 1003,
        HttpGetServerInfoReq = 1004,
        HttpGetServerInfoRsp = 1005,
        G2C_RoleKickoutMsg = 100000,
        C2G_RoleLoginCheckReq = 100001,
        C2G_RoleLoginCheckRsp = 100002,
        C2G_RoleLoginReq = 100003,
        C2G_RoleLoginRsp = 100004,
        C2G_EnterSceneReq = 100006,
        C2G_EnterSceneRsp = 100007,
        G2C_Role_InfoMsg = 101000,
        /// <summary>
        /// 角色的物品信息
        /// </summary>
        G2C_Item_InfoMsg = 102000,
    }
    
}