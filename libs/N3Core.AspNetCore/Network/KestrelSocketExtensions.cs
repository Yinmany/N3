using System.Buffers;
using System.Reflection;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace N3Core.AspNetCore;

public static class KestrelSocketExtensions
{
    const string TypeName = "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.SocketConnectionFactory";

    /// <summary>
    /// 注册SocketConnectionFactory为IConnectionFactory
    /// 提供内部TcpClientSocket使用
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddSocketConnectionFactory(this IServiceCollection services)
    {
        var factoryType = typeof(SocketTransportOptions).Assembly.GetType(TypeName);
        return factoryType == null
            ? throw new NotSupportedException($"找不到类型{TypeName}")
            : services.AddSingleton(typeof(IConnectionFactory), factoryType);
    }

    /// <summary>
    /// 从SocketTransportOptions中获取
    /// </summary>
    /// <returns></returns>
    public static MemoryPool<byte> CreateFromPinnedBlockMemoryPoolFactory()
    {
        var factoryType = typeof(SocketTransportOptions).Assembly.GetType("System.Buffers.PinnedBlockMemoryPoolFactory");
        if (factoryType is null)
            throw new NotSupportedException($"找不到类型{TypeName}");

        MethodInfo? createMethodInfo = factoryType.GetMethod("Create");
        if (createMethodInfo is null)
            throw new NotSupportedException($"找不到类型{TypeName}方法Create");
        return (MemoryPool<byte>)createMethodInfo.Invoke(null, null)!;
    }
}