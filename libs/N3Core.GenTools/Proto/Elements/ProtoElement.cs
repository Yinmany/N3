namespace N3Core.GenTools;

public abstract class ProtoElement
{
    public List<ProtoElement> Elems = new List<ProtoElement>();

    public ProtoElement Parent { get; private set; }

    public string Type;

    public ProtoElement()
    {
        Type = GetType().Name;
    }

    public void AddChild(ProtoElement child)
    {
        Elems.Add(child);
        child.Parent = this;
    }
}