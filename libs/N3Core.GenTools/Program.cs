using System.Text.Json;
using System.Xml.Serialization;
using CommandLine;

namespace N3Core.GenTools;

public class Program
{
    public class Options
    {
        // 输入目录
        [Option('f', "conf", Required = false, Default = "conf.xml")]
        public string Conf { get; set; }
    }

    public static int Main(string[] args)
    {
        ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);
        Options options = result.Value;

        if (!File.Exists(options.Conf))
        {
            SLog.Error($"配置文件不存在: {options.Conf}");
            return 1;
        }

        SLog.Info($"conf: {options.Conf}");

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GenConfig));
            GenConfig genConfig = (GenConfig)serializer.Deserialize(File.OpenRead(options.Conf));
            ParseVar(genConfig);

            if (genConfig is null)
            {
                SLog.Error($"配置文件解析失败: {options.Conf}");
                return 1;
            }

            if (genConfig.Proto is { Length: > 0 })
            {
                foreach (var proto in genConfig.Proto)
                {
                    if (!ProtoGen.Gen(proto))
                    {
                        return 1;
                    }
                }
            }

            if (genConfig.Enum is { Length: > 0 })
            {
                foreach (var item in genConfig.Enum)
                    EnumGen.Gen(item);
            }

            if (genConfig.Handler is { Length: > 0 })
            {
                foreach (var item in genConfig.Handler)
                    HandlerGen.Gen(item);
            }

            return 0;
        }
        catch (Exception e)
        {
            SLog.Error(e);
            return 1;
        }
    }

    private static void ParseVar(GenConfig genConfig)
    {
        if (genConfig is { Proto.Length: > 0 })
        {
            SLog.Info($"=== 解析Proto变量 ===");
            foreach (ProtoConfig config in genConfig.Proto)
            {
                config.In = ParseVar(config.In, genConfig.Vars);
                config.Out = ParseVar(config.Out, genConfig.Vars);
                config.TplBase = ParseVar(config.TplBase, genConfig.Vars);
                config.Tpl = ParseVar(config.Tpl, genConfig.Vars);
                config.MsgId = ParseVar(config.MsgId, genConfig.Vars);

                SLog.Info(JsonSerializer.Serialize(config));
            }
        }

        if (genConfig is { Handler.Length: > 0 })
        {
            SLog.Info($"=== 解析Handler变量 ===");
            foreach (HandlerConfig config in genConfig.Handler)
            {
                if (!string.IsNullOrEmpty(config.Usings))
                    config.UsingArrary = config.Usings.Split(";").ToArray();

                foreach (var item in config.GenConfig)
                {
                    item.Out = ParseVar(item.Out, genConfig.Vars);
                    item.Prefix = ParseVar(item.Prefix, genConfig.Vars);
                    if (!string.IsNullOrEmpty(item.Id))
                    {
                        string[] ids = item.Id.Split(",");
                        item.Ids = ids.Select(x => int.Parse(x)).ToArray();
                    }

                    if (!string.IsNullOrEmpty(item.Exclude))
                    {
                        string[] ids = item.Exclude.Split(",");
                        item.ExcludeIds = ids.Select(x => int.Parse(x)).ToArray();
                    }
                }

                //SLog.Info(JsonSerializer.Serialize(config));
            }
        }

        if (genConfig is { Enum.Length: > 0 })
        {
            SLog.Info($"=== 解析Enum变量 ===");
            foreach (EnumConfig config in genConfig.Enum)
            {
                config.In = ParseVar(config.In, genConfig.Vars);
                config.Out = ParseVar(config.Out, genConfig.Vars);
                config.TplBase = ParseVar(config.TplBase, genConfig.Vars);
                config.Tpl = ParseVar(config.Tpl, genConfig.Vars);
                SLog.Info(JsonSerializer.Serialize(config));
            }
        }

        SLog.Info(""); // 空行
    }

    private static string ParseVar(string str, VarConfig[] vars)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        foreach (VarConfig item in vars)
        {
            str = str.Replace($"${{{item.Name}}}", item.Value);
        }

        return str;
    }
}