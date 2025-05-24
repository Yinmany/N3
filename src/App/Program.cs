using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using N3;
using NLog;
using System.Net;
using Cysharp.Threading.Tasks;
using ProjectX;
using ProjectX.DB;

try
{
    UniTaskScheduler.UnobservedTaskException += UniTaskScheduler_UnobservedTaskException;

    ushort nodeId = 1;
    SLog.Init(name => new NLogAdapter(name), "");
    ServerConfig.Init("./server.xml", nodeId);

    var list = ServerConfig.GetAllByNodeId(nodeId);
    if (list is null or { Count: 0 })
        throw new Exception("No server config");

    if (!ServerConfig.GlobalKv.TryGetValue("db", out var dbConnStr))
        throw new Exception("GlobalKv中没有db连接配置");
    if (!ServerConfig.GlobalKv.TryGetValue("rdb", out var rdbConnStr))
        throw new Exception("GlobalKv中没有rdb连接配置");

    // 是否存在登录服
    bool isLoginSrv = list.Any(x => x.Type == ServerType.Login);
    if (isLoginSrv && LoginServer.CheckServerConfig() != 0)
        return;
    if (isLoginSrv)
    {
        //AccountDb.Init(dbConnStr, rdbConnStr);
    }

    // 是否存在非登录服的其它服
    bool isGameSrv = list.Any(x => x.Type != ServerType.Login);
    if (isGameSrv)
    {
        //GameDb.Init(dbConnStr, rdbConnStr);
    }

    // 注册序列化器
    var objectSerializer = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);
    BsonSerializer.RegisterSerializer(objectSerializer);

    AssemblyPartManager.Ins
        .AddPart(typeof(MsgId).Assembly)
        .AddHotfixPart("WorldSrv.Hotfix")
        .AddHotfixPart("GameSrv.Hotfix")
        .AddHotfixPart("GateSrv.Hotfix")
        .EnableWatch(false)
        .Load();

    //  本地节点Listen
    IPEndPoint? localNodeBindIp = ServerConfig.GetNodeIp(nodeId);
    if (localNodeBindIp != null)
        MessageCenter.Ins.Listen(localNodeBindIp);

    foreach (var cfg in list)
    {
        ServerApp? app = null;
        app = cfg.Type switch
        {
            ServerType.World => new WorldServer(cfg.Id, cfg.Type, cfg.Name),
            ServerType.Game => new GameServer(cfg.Id, cfg.Type, cfg.Name),
            ServerType.Gate => new GateServer(cfg.Id, cfg.Type, cfg.Name),
            ServerType.Login => new LoginServer(cfg.Id, cfg.Type, cfg.Name),
            _ => new ServerApp(cfg.Id, cfg.Type, cfg.Name),
        };

        if (app != null)
        {
            PosixSignalHook.Ins.AddStopCallback(app.Shutdown);
            SLog.Info($"创建ServerApp: {cfg.Id} {cfg.Type} {cfg.Name} {Did.Make(cfg.Id, ServerConfig.LocalNodeId)}");
        }
    }

    await PosixSignalHook.Ins.WaitForExitAsync();
}
catch (Exception ex)
{
    SLog.Error(ex, "Application terminated unexpectedly");
}
finally
{
    PosixSignalHook.Ins.Dispose();
    LogManager.Shutdown();
}

void UniTaskScheduler_UnobservedTaskException(Exception obj)
{
    SLog.Error(obj, "UnobservedTaskException");
}

