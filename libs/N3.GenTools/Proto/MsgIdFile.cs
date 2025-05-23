namespace N3.GenTools;

public static class MsgIdFile
{
    private static readonly Dictionary<string, ProtoEnumField> _values = new();

    public static IReadOnlyDictionary<string, ProtoEnumField> Values => _values;

    public static int Count => _values.Count;
    public static ProtoEnumField? Get(string name) => _values.GetValueOrDefault(name);

    public static string Name { get; private set; }

    public static string NameSpace { get; private set; }

    public static void Load(string path)
    {
        if (!File.Exists(path))
            throw new Exception($"文件不存在: {path}");

        _values.Clear();

        string[] lines = File.ReadAllLines(path);
        bool isStart = false;
        foreach (var line in lines)
        {
            string tmp = line.Trim();
            if (string.IsNullOrEmpty(tmp))
                continue;

            if (tmp.StartsWith("option"))
            {
                string[] arr = tmp.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (arr[1] == "csharp_namespace")
                {
                    string ns = tmp.Split("=")[1];
                    NameSpace = ns.Replace(";", "").Replace("\"", "").Trim();
                }

                continue;
            }

            if (tmp.StartsWith("enum"))
            {
                isStart = true;
                Name = tmp.Split(" ", StringSplitOptions.RemoveEmptyEntries)[1];
                continue;
            }

            if (!isStart)
                continue;

            if (tmp.StartsWith('{') || tmp.StartsWith('}'))
                continue;

            if (tmp.StartsWith("//"))
                continue;

            ProtoEnumField field = new ProtoEnumField(tmp);
            _values.Add(field.Name, field);
        }
    }
}