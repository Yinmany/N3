namespace N3Core.GenTools;

public class ProtoEnum : ProtoElement
{
    public string Name { get; }

    public ProtoEnum(string line)
    {
        Name = line.Split(ProtoFile.splitChars, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
    }
}

public class ProtoEnumField : ProtoElement
{
    public readonly string Name;
    public readonly int    Index;
    public readonly string TailComment;

    public ProtoEnumField(string line)
    {
        // 尾部注释
        int index = line.IndexOf(";");
        TailComment = line.Substring(index + 1).Replace("//", "").Trim();

        // 移除;
        line = line.Remove(index);

        string[] ss = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
        Name = ss[0].Trim();

        string i = ss[1].Trim();
        int.TryParse(i, out Index);
    }
}