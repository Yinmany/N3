using System.Text.Json;

namespace N3;

public static class MessageLog
{
    public static void DebugMsg<T>(this IMsgHandlerBase self, T msg) where T : IMessage
    {
        SLog.Debug($"{self.GetType().Name} {JsonSerializer.Serialize(msg)}");
    }
}
