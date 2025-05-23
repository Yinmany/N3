namespace N3.GenTools;

public class ProtoMessageOption : ProtoElement
{
    public string DefineStr { get; private set; }

    public ProtoMessageOption(string line)
    {
        string[] tmp = line.Split(';')[0].Split('=');
        string defineStr = tmp[1];
        this.DefineStr = defineStr.Trim();
    }
}