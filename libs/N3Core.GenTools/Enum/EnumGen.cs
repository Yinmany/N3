using System.Xml;
using Scriban;

namespace N3Core.GenTools;

public static class EnumGen
{
    private static Template _tpl;

    public static void Gen(EnumConfig config)
    {
        SLog.Info("==== 开始生成Enum ====");

        _tpl = TplHelper.Load(config.Tpl, config.TplBase);
        if (!File.Exists(config.In))
            throw new Exception($"xml文件不存在: {config.In}");

        XmlDocument doc = new();
        doc.Load(config.In);

        var root = doc.DocumentElement;
        if (root == null)
            throw new Exception($"xml格式错误: {config.In}");

        string typeName = root.GetAttribute("name");
        string ns = root.GetAttribute("namespace");
        string i18n = root.GetAttribute("i18n");
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(ns))
        {
            throw new Exception($"name 与 namespace 必须填写: name={typeName} namespace={ns}");
        }

        bool isI18N = false;
        string[] i18nArr = null;
        if (!string.IsNullOrEmpty(i18n))
        {
            i18nArr = i18n.Split(',', StringSplitOptions.RemoveEmptyEntries);
            isI18N = i18nArr.Length > 0;
        }
        Console.WriteLine($"i18n: {i18n} {isI18N} {i18nArr?.Length}");

        Dictionary<int, EnumItem> items = new Dictionary<int, EnumItem>();
        int startIndex = 0;
        foreach (XmlNode xmlNode in root.ChildNodes)
        {
            if (xmlNode.Name is not "var")
                continue;

            string name = xmlNode.Attributes["name"].Value;
            string comment = xmlNode.Attributes["comment"]?.Value;
            string value = xmlNode.Attributes["value"]?.Value;

            if (value != null && int.TryParse(value, out startIndex))
            {
                SLog.Info($"设置 index = {startIndex}");
            }

            if (items.ContainsKey(startIndex))
                throw new Exception($"value重复: {name} = {value} // {comment}");

            string[] loc = null;
            if (isI18N)
            {
                loc = new string[i18nArr.Length];
                for (int i = 0; i < i18nArr.Length; i++)
                {
                    string key = i18nArr[i];
                    string v = xmlNode.Attributes[key]?.Value;
                    loc[i] = v;
                    //comment += $"\n        ///{key}: {v}\n";
                    //SLog.Info($"{key} = {v}");
                }
            }

            items.Add(startIndex, new EnumItem(name, startIndex, comment, loc));
            ++startIndex;
        }

        string str = _tpl.Render(new EnumData { Namespace = ns, Name = typeName, Items = items.Values.ToArray(), Language = i18nArr, IsI18n = config.EnableI18n });

        string fileExt = Path.GetFileNameWithoutExtension(config.Tpl).Split('_')[^1];

        string outPath = Path.Combine(config.Out, $"{typeName}.{fileExt}");
        File.WriteAllText(outPath, str);
        Console.WriteLine($"out => {outPath}");

        SLog.Info("==== 生成Enum成功 ====");
    }
}