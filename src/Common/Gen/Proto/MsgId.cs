using ProtoBuf;
using N3;
using System.Collections.Generic;

namespace ProjectX
{
    public enum MsgId
    {
        None = 0,
        PingReq = 10010,
        PingRsp = 10020,
        HttpLoginVerifyReq = 10100,
        HttpLoginVerifyRsp = 10110,
        HttpGetNoticeRsp = 10120,
        HttpGetServerInfoReq = 10130,
        HttpGetServerInfoRsp = 10140,
        C2G_Login_GateReq = 10203,
        C2G_Login_GateRsp = 10213,
    }
    
}