﻿using Cysharp.Threading.Tasks;
using N3;
using ProjectX;

[assembly: ServerType(ServerType.World), ServerInit(typeof(WorldSrvInit))]

namespace ProjectX;

public class WorldSrvInit : IServerInit
{
    public UniTask OnInit(ServerApp app)
    {
        app.AddComp(new MessageInbox());
        app.AddComp(new ServerDiscover()); // 用于管理

        SLog.Info("OnInit");
        return UniTask.CompletedTask;
    }

    public UniTask OnUnInit(ServerApp app)
    {
        SLog.Info("OnUnInit");
        return UniTask.CompletedTask;
    }
}