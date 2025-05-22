using System.Text;
using Scriban;

namespace N3Core.GenTools;

public static class ProtoGen
{
    private static Template _headTpl;
    private static Template _enumTpl;
    private static Template _msgTpl;
    private static ProtoConfig _config;
    private static bool _isMsgId;

    public static bool Gen(ProtoConfig config)
    {
        SLog.Info($"==== 生成Proto开始 ====");
        SLog.Info($"gen proto: {config.In}");

        _config = config;
        _headTpl = TplHelper.Load(Path.Combine(_config.Tpl, "head.tpl"), _config.TplBase);
        _enumTpl = TplHelper.Load(Path.Combine(_config.Tpl, "enum.tpl"), _config.TplBase);
        _msgTpl = TplHelper.Load(Path.Combine(_config.Tpl, "message.tpl"), _config.TplBase);

        // 载入消息id
        if (!string.IsNullOrEmpty(config.MsgId))
        {
            string msgIdFilePath = Path.Combine(config.In, config.MsgId);
            MsgIdFile.Load(msgIdFilePath);
            SLog.Info($"load msg_id: {MsgIdFile.Count} {config.MsgId}");
            _isMsgId = true;
        }
        else
        {
            _isMsgId = false;
        }

        // 解析proto文件
        if (!Directory.Exists(config.Out))
            Directory.CreateDirectory(config.Out);
        string[] files = Directory.GetFiles(config.In, "*.proto");

        bool isOk = true;
        foreach (var file in files)
        {
            isOk &= GenProto(file);
        }

        SLog.Info("==== 生成Proto结束 ====");
        SLog.Info("");
        return isOk;
    }


    static bool GenProto(string file)
    {
        StringBuilder stringBuilder = new StringBuilder();

        string protoTxt = File.ReadAllText(file);
        var data = ProtoFile.Parse(protoTxt);

        foreach (var e in data.Elems)
        {
            Template curTpl = null;
            if (e is ProtoEnum)
            {
                curTpl = _enumTpl;
            }
            else if (e is ProtoMessage protoMessage)
            {
                curTpl = _msgTpl;

                bool isNetMessage = protoMessage.Name.EndsWith("Msg") ||
                                    protoMessage.Name.EndsWith("Req") ||
                                    protoMessage.Name.EndsWith("Rsp");

                if (_isMsgId) // 配置了msgId文件才会对消息id进行处理
                {
                    ProtoElement msgId = protoMessage.Elems.FirstOrDefault(f => f.GetType() == typeof(ProtoMessageOption));
                    ProtoEnumField msgIdDefine = null;
                    if (msgId is ProtoMessageOption option)
                    {
                        msgIdDefine = MsgIdFile.Get(option.DefineStr);
                        if (msgIdDefine == null && isNetMessage)
                        {
                            SLog.Error($"未找到MsgId: {protoMessage.Name} {option.DefineStr}");
                        }
                    }
                    else
                    {
                        msgIdDefine = MsgIdFile.Get(protoMessage.Name);
                        if (msgIdDefine == null && isNetMessage) // 不然就不提示了
                        {
                            SLog.Error($"未找到MsgId: {protoMessage.Name}");
                        }
                    }

                    if (msgIdDefine != null)
                    {
                        protoMessage.Opcode = $"(int){MsgIdFile.NameSpace}.{MsgIdFile.Name}.{msgIdDefine.Name}";
                    }
                }

                // 嵌套枚举
                StringBuilder temp = new StringBuilder();
                foreach (var inner in protoMessage.Elems)
                {
                    if (inner is ProtoEnum)
                    {
                        temp.AppendLine(_enumTpl.Render(inner));
                    }
                }

                protoMessage.EnumCodes = temp.ToString();
            }

            if (curTpl != null)
            {
                try
                {
                    stringBuilder.AppendLine(curTpl.Render(e));
                }
                catch (Exception exception)
                {
                    SLog.Error(exception);
                    return false;
                }
            }
        }

        data.Codes = stringBuilder.ToString();
        string finalCodes = _headTpl.Render(data);

        string fileName = Path.GetFileNameWithoutExtension(file);
        string fileExt = _config.Tpl.Split('_')[^1];
        string outputFileName = Path.Combine(_config.Out, $"{fileName}.{fileExt}");
        File.WriteAllText(outputFileName, finalCodes);
        SLog.Info($"out => {outputFileName}");
        return true;
    }
}