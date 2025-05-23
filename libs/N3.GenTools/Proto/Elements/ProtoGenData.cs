
namespace N3.GenTools;

public class ProtoGenData : Stack<ProtoElement>
{
    public string Namespace { get; set; }
    
    public List<string> UsingList { get; } = new(); // 自定义使用的命名空间

    public readonly List<ProtoElement> Elems = new();

    public string Codes { get; set; }
    
    public ProtoElement TryPeek()
    {
        if (this.Count == 0)
            return null;
        return this.Peek();
    }
}