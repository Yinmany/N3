namespace N3.GenTools;

public class ProtoMessage : ProtoElement
{
    public string Name { get; }
    public string ParentClass { get; } = string.Empty;

    public string Opcode { get; set; }
    
    public string EnumCodes { get; set; }
    
    public int AckOpcode { get; set; }
    
    public string Comment { get; set; }
    
    public ProtoMessage(string line)
    {
        Name = line.Split(ProtoFile.splitChars, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
        string[] ss = line.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);
        if (ss is { Length: > 1 })
        {
            ParentClass = ss[1].Trim();
        }
    }
}

