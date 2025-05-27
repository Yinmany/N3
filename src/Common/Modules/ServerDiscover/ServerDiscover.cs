using Cysharp.Threading.Tasks;
using MongoDB.Bson;
using N3;
using System.Net;

namespace ProjectX;

/// <summary>
/// 服务发现
/// </summary>
public class ServerDiscover : AComponent
{
    public static SLogger logger = new SLogger("ServerDiscover");

    private readonly Dictionary<uint, List<PbServerInfo>> _infos = new Dictionary<uint, List<PbServerInfo>>();
    private ServerConfig _worldServerConfig;
    private Did _worldServerActorId;
    public bool IsMaster => this.EntityAs<ServerApp>()?.ServeType == ServerType.World;

    public void Init()
    {
        ServerConfig? worldServerConfig = ServerConfig.FindOneByServerType(ServerType.World);
        if (worldServerConfig == null)
            throw new Exception("找不到World服务器配置.");
        _worldServerConfig = worldServerConfig;
        _worldServerActorId = Did.Make(_worldServerConfig.Id, _worldServerConfig.NodeId);

        PbServerInfo serverInfo = new PbServerInfo
        {
            SrvType = ServerType.World,
            ActorId = _worldServerActorId,
            NodeId = worldServerConfig.NodeId,
            NodeIp = ServerConfig.GetNodeIp(worldServerConfig.NodeId)!.ToString(),
        };
        AddServerInfo(serverInfo);
        RegisterAsync().Forget();
    }

    private async UniTask RegisterAsync()
    {
        await Task.Delay(2500); // 2.5s后注册

        ServerApp serverApp = this.RootAs<ServerApp>();
        IPEndPoint ip = ServerConfig.GetNodeIp(ServerConfig.LocalNodeId)!;
        PbServerInfo serverInfo = new PbServerInfo
        {
            SrvType = serverApp.ServeType,
            ActorId = serverApp.Id,
            NodeId = ServerConfig.LocalNodeId,
            NodeIp = ip.ToString(),
        };

        //logger.Info($"正在注册: {serverInfo.ToJson()}...");
        A2W_Server_AppRsp rsp = await MessageCenter.Ins.Call<A2W_Server_AppRsp>(_worldServerActorId, new A2W_Server_AppReq { ServerInfo = serverInfo, Op = 1 });
        //logger.Info($"注册成功: {serverInfo.ToJson()}...ok!");

    }

    private void AddServerInfo(PbServerInfo serverInfo)
    {
        if (!_infos.TryGetValue(serverInfo.SrvType, out List<PbServerInfo>? list))
        {
            list = new List<PbServerInfo>();
            _infos.Add(serverInfo.SrvType, list);
        }
        list.Add(serverInfo);
        MessageCenter.Ins.AddNode((ushort)serverInfo.NodeId, IPEndPoint.Parse(serverInfo.NodeIp));
        logger.Info($"{this.EntityAs<ServerApp>().Name} {this.Entity.Id} add {serverInfo.ToJson()}");

        if (this.IsMaster)
        {
            W2A_Server_AppMsg addMsg = new W2A_Server_AppMsg();
            addMsg.Op = 1;
            addMsg.ServerInfo = serverInfo;
            foreach (var kv in _infos)
            {
                if (kv.Key == ServerType.World)
                    continue;
                foreach (var info in kv.Value)
                {
                    if (info.ActorId == serverInfo.ActorId)
                        continue;

                    MessageCenter.Ins.Send(info.ActorId, addMsg);
                    MessageCenter.Ins.Send(serverInfo.ActorId, new W2A_Server_AppMsg { ServerInfo = info, Op = 1 });
                }
            }
        }
    }

    public void ServerInfoChanged(PbServerInfo serverInfo, int op)
    {
        if (op == 1)
        {
            this.AddServerInfo(serverInfo);
        }
        else // 移除
        {

        }
    }
}