using System.Net;
using Cysharp.Threading.Tasks;
using N3;
using ProjectX;

[assembly: ServerType(ServerType.Game), ServerInit(typeof(GameSrvInit))]

namespace ProjectX;

public class GameSrvInit : IServerInit
{
    public async UniTask OnInit(ServerApp app)
    {
        SLog.Info("OnInit");
        //ServerConfig? serverConfig = ServerConfig.GetConfig(Did.LocalNodeId, app.ServerId);
        //if (serverConfig is null)
        //{
        //    SLog.Error($"游戏服配置未找到: {Did.LocalNodeId} {app.ServerId}");
        //    return;
        //}

        //string? listen = serverConfig.Kv.GetValueOrDefault("listen");
        //if (listen is null)
        //{
        //    SLog.Error($"游戏服listen配置不存在: {Did.LocalNodeId} {app.ServerId} listen");
        //    return;
        //}

        app.AddComp(new ActorComp());
        ClusterComp cluster = app.AddComp(new ClusterComp());
        await cluster.RegisterAsync();

        


        EventSystem eventSystem = app.GetComp<EventSystem>();
        eventSystem.AddInterval(TimeSpan.FromSeconds(5), InvokeId.GameServerInfoTimer, app);

    }

    public UniTask OnUnInit(ServerApp app)
    {
        SLog.Info("OnUnInit");
        return UniTask.CompletedTask;
    }
}