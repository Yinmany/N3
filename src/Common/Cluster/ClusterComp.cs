using Cysharp.Threading.Tasks;
using N3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProjectX;

/// <summary>
/// 集群组件
/// </summary>
public class ClusterComp : AComponent
{
    public async UniTask RegisterAsync()
    {
        ServerConfig? worldServerConfig = ServerConfig.FindOneByServerType(ServerType.World);
        if (worldServerConfig == null)
            throw new Exception("找不到World服务器配置.");

        ServerApp serverApp = this.RootAs<ServerApp>();
        IPEndPoint ip = ServerConfig.GetNodeIp(ServerConfig.LocalNodeId)!;
        long dstId = Did.Make(worldServerConfig.Id, worldServerConfig.NodeId);
        A2W_SD_AddRsp rsp = await Call<A2W_SD_AddRsp>(dstId, new A2W_SD_AddReq { SrvType = serverApp.ServeType, NodeId = ServerConfig.LocalNodeId, NodeIp = ip.ToString() });
    }

    public static void Send(long id, IMessage msg)
    {
        MessageCenter.Ins.Send(id, msg);
    }

    public static ValueTask<IResponse> Call(long id, IRequest request, short timeout = 60)
    {
        return MessageCenter.Ins.Call(id, request, timeout);
    }

    public static async UniTask<TRsp> Call<TRsp>(long id, IRequest request, short timeout = 60) where TRsp : class, IResponse
    {
        TRsp rsp = (TRsp)await MessageCenter.Ins.Call(id, request, timeout);
        return rsp;
    }
}
