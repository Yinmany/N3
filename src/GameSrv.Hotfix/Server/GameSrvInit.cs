using System.Net;
using Cysharp.Threading.Tasks;
using N3;

[assembly: ServerType(ServerType.Game), ServerInit(typeof(GameSrvInit))]

namespace N3;

public class GameSrvInit : IServerInit
{
    public UniTask OnInit(ServerApp app)
    {
        SLog.Info("OnInit");
        ServerConfig? serverConfig = ServerConfig.GetConfig(Did.LocalNodeId, app.ServerId);
        if (serverConfig is null)
        {
            SLog.Error($"游戏服配置未找到: {Did.LocalNodeId} {app.ServerId}");
            return UniTask.CompletedTask;
        }

        string? listen = serverConfig.Kv.GetValueOrDefault("listen");
        if (listen is null)
        {
            SLog.Error($"游戏服listen配置不存在: {Did.LocalNodeId} {app.ServerId} listen");
            return UniTask.CompletedTask;
        }

        app.AddComp(new ActorComp());

        NetworkComp.EnableDebug = true;
        NetworkComp network = app.AddComp(new NetworkComp());
        network.Listen(IPEndPoint.Parse(listen));

        EventSystem eventSystem = app.GetComp<EventSystem>();
        eventSystem.AddInterval(TimeSpan.FromSeconds(5), InvokeId.GameServerInfoTimer, app);


        return UniTask.CompletedTask;
    }

    public UniTask OnUnInit(ServerApp app)
    {
        SLog.Info("OnUnInit");
        return UniTask.CompletedTask;
    }
}