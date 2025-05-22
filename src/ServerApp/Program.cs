using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using N3Core;
using N3Lib;
using NLog;
using System.Net;
using N3;

try
{
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
    if (isLoginSrv && LoginSrvApp.CheckServerConfig() != 0)
        return;

    // 注册序列化器
    var objectSerializer = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);
    BsonSerializer.RegisterSerializer(objectSerializer);

    AssemblyPartManager.Ins
        .AddPart(typeof(MsgId).Assembly)
        .AddHotfixPart("GameSrv.Hotfix")
        .AddHotfixPart("WorldSrv.Hotfix")
        .EnableWatch(false)
        .Load();

    //  添加本地节点
    IPEndPoint? localNodeBindIp = ServerConfig.GetNodeIp(nodeId);
    if (localNodeBindIp != null)
        MessageCenter.Ins.AddNode(nodeId, localNodeBindIp);

    // 注册中心服节点
    RegisterWorldSrvNode();

    foreach (var cfg in list)
    {
        if (cfg.Type == ServerType.Login)
        {
            LoginSrvApp app = new LoginSrvApp(cfg.Id, cfg.Type, cfg.Name);
            PosixSignalHook.Ins.AddStopCallback(app.WaitForShutdownAsync);
        }
        else
        {
            ServerApp app = new ServerApp(cfg.Id, cfg.Type, cfg.Name);
            PosixSignalHook.Ins.AddStopCallback(app.Shutdown);
        }

        SLog.Info($"创建ServerApp: {cfg.Id} {cfg.Type} {cfg.Name}");
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

void RegisterWorldSrvNode()
{
    // 注册中心服
    ServerConfig? worldConfig = ServerConfig.FindOneByServerType(ServerType.World);
    if (worldConfig is null)
    {
        SLog.Error("world server not found");
        return;
    }

    IPEndPoint? worldIp = ServerConfig.GetNodeIp(worldConfig.NodeId);
    if (worldIp is null)
    {
        SLog.Error("world server ip not found");
        return;
    }
    if (ServerConfig.LocalNodeId != worldConfig.NodeId)
        MessageCenter.Ins.AddNode(worldConfig.NodeId, worldIp);
    SLog.Info($"注册中心服: {worldConfig.NodeId}, {worldIp}");
}