using NLog.Extensions.Logging;
using ProjectX.Login;
using N3;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProjectX;

public class LoginServer : ServerApp
{
    private readonly Task _waitShutdownTask;

    public LoginServer(ushort serverId, ushort serverType, string name) : base(serverId, serverType, name)
    {
        // 创建web服务器
        var builder = WebApplication.CreateBuilder(Environment.GetCommandLineArgs());
        builder.Logging.ClearProviders().AddNLog();

        ConfigurationManager configuration = builder.Configuration;
        configuration.AddYamlFile("login-conf.yaml", false, true);
        builder.Services.Configure<ServerList>(configuration.GetSection("server_list"));

        var services = builder.Services;
        services.AddSingleton<LoginMod>();

        services.AddControllers();

        services.AddOpenApi();

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.Services.GetRequiredService<LoginMod>();

        app.MapControllers();
        _waitShutdownTask = app.RunAsync();
    }

    public static int CheckServerConfig()
    {
        ServerConfig? loginConf = ServerConfig.FindOneByServerType(ServerType.Login);
        if (loginConf is null)
        {
            SLog.Error("登录服配置不存在!");
            return 1;
        }

        if (!loginConf.Kv.ContainsKey("db"))
        {
            SLog.Error("Db数据库配置不存在!");
            return 2;
        }

        if (!loginConf.Kv.ContainsKey("rdb"))
        {
            SLog.Error("Rdb数据库配置不存在!");
            return 3;
        }

        return 0;
    }

    protected override async Task OnShutdownAsync()
    {
        await _waitShutdownTask;
        await base.OnShutdownAsync();
    }
}