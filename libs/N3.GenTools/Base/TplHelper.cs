using Scriban;

namespace N3.GenTools;

public static class TplHelper
{
    /// <summary>
    /// 加载模板
    /// </summary>
    /// <param name="path"></param>
    /// <param name="basePath"></param>
    /// <returns></returns>
    public static Template Load(string path, string basePath = null)
    {
        basePath ??= AppDomain.CurrentDomain.BaseDirectory;
        if (!Path.IsPathRooted(path) && path != null)
            path = Path.Combine(basePath, path);

        if (!File.Exists(path))
        {
            throw new Exception($"模板文件不存在: {path}");
        }

        string tpl = File.ReadAllText(path);
        var template = Template.Parse(tpl);

        if (template.HasErrors)
        {
            foreach (var msg in template.Messages)
            {
                SLog.Error($"模板错误: {msg} {path}");
            }

            throw new Exception($"模板错误!");
        }

        return template;
    }
}