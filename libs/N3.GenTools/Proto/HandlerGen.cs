using Scriban;
using System.Text.Json;

namespace N3.GenTools;

public class HandlerGen
{
    private static Template _asyncMsgTpl;
    private static Template _asyncReqTpl;

    public static void Gen(HandlerConfig config)
    {
        if (string.IsNullOrEmpty(config.MsgId))
            return;
        if (!File.Exists(config.MsgId))
            return;

        SLog.Info($"==== 生成 Proto.Handler 开始 ====");

        MsgIdFile.Load(config.MsgId);
        SLog.Info($"load msg_id: {MsgIdFile.Count} {config.MsgId}");

        _asyncMsgTpl = TplHelper.Load(Path.Combine(config.Tpl, "async_msg_handler.tpl"));
        _asyncReqTpl = TplHelper.Load(Path.Combine(config.Tpl, "async_req_handler.tpl"));

        // 匹配生成配置
        foreach (var item in config.GenConfig)
        {
            foreach (var kv in MsgIdFile.Values)
            {
                if (!Match(kv.Value, item))
                    continue;
                GenOne(kv.Value, item, config);
            }
        }

        SLog.Info($"==== 生成 Proto.Handler 结束 ====");
    }

    private static bool Match(ProtoEnumField field, HandlerGenConfig config)
    {
        // 先排除
        if (config is { ExcludeIds.Length: > 0 } && config.ExcludeIds.Contains(field.Index))
            return false;

        // 匹配Id
        if (config.Ids is { Length: > 0 } && !config.Ids.Contains(field.Index))
            return false;

        // 配置Type
        if (config.Type != field.Index / 1000)
            return false;

        // 匹配前缀
        if (config.Prefix.Length > 0 && !field.Name.StartsWith(config.Prefix))
            return false;

        return true;
    }

    /// <summary>
    /// 生成单个
    /// </summary>
    /// <param name="field"></param>
    /// <param name="config"></param>
    private static void GenOne(ProtoEnumField field, HandlerGenConfig config, HandlerConfig f)
    {
        string fileName = field.Name + "Handler.cs";
        string filePath = Path.Combine(config.Out, fileName);
        if (File.Exists(filePath))
            return;

        if(!Directory.Exists(config.Out))
            Directory.CreateDirectory(config.Out);

        // 消息
        string finalCodes;
        HandlerGenData data = new HandlerGenData();
        data.Namespace = f.Namespace;
        data.CtxType = config.ContextType;
        data.MsgType = field.Name;
        data.Usings = f.UsingArrary;

        if (field.Name.EndsWith("Msg"))
        {
            finalCodes = _asyncMsgTpl.Render(data);
        }
        else if (field.Name.EndsWith("Req"))
        {
            data.RspType = field.Name.Substring(0, field.Name.Length - 3) + "Rsp";
            finalCodes = _asyncReqTpl.Render(data);
        }
        else
        {
            return;
        }

        SLog.Info(JsonSerializer.Serialize(data));

        File.WriteAllText(filePath, finalCodes);
        SLog.Info($"out => {filePath}");
    }
}

public class HandlerGenData
{
    public string Namespace { get; set; }
    public string[] Usings { get; set; }
    public string MsgType { get; set; }
    public string CtxType { get; set; }
    public string RspType { get; set; }
}