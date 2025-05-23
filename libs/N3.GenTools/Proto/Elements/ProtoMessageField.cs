namespace N3.GenTools;

public class ProtoMessageField : ProtoElement
{
    public readonly string Name;
    public readonly string FieldType;

    public readonly bool IsRepeated;
    public readonly bool IsOptional;

    public readonly string TailComment;

    public readonly int Index = 0;

    public ProtoMessageField(string line)
    {
        IsRepeated = line.StartsWith("repeated");

        // 尾部注释
        int index = line.IndexOf(";");
        TailComment = line.Substring(index + 1).Replace("//", "").Trim();

        // 移除;
        line = line.Remove(index);

        string[] ss = line.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);
        if (ss is { Length: 5 })
        {
            string optional = ss[0];
            string type = ss[1];

            this.Name = ss[2];
            this.Index = int.Parse(ss[4]);

            this.FieldType = ConvertType(type);
            IsOptional = "optional" == optional.Trim();
        }
        else
        {
            string type = ss[0];
            this.Name = ss[1];
            this.Index = int.Parse(ss[3]);
            this.FieldType = ConvertType(type);
        }

        // 下划线转驼峰
        if (this.Name.Contains("_"))
        {
            string[] nameArr = this.Name.Split('_');
            this.Name = string.Join("", nameArr.Select(s => char.ToUpper(s[0]) + s.Substring(1)));
        }
        else
        {
            this.Name = char.ToUpper(this.Name[0]) + this.Name.Substring(1);
        }
    }

    private static string ConvertType(string type)
    {
        string typeCs = "";
        switch (type)
        {
            case "int16":
                typeCs = "short";
                break;
            case "int32":
                typeCs = "int";
                break;
            case "bytes":
                typeCs = "byte[]";
                break;
            case "uint32":
                typeCs = "uint";
                break;
            case "long":
                typeCs = "long";
                break;
            case "int64":
                typeCs = "long";
                break;
            case "uint64":
                typeCs = "ulong";
                break;
            case "uint16":
                typeCs = "ushort";
                break;
            default:
                typeCs = type;
                break;
        }

        return typeCs;
    }
}