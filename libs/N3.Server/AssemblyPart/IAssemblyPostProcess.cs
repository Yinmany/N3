namespace N3;

public interface IAssemblyPostProcess
{
    void Begin();

    void Process(ushort serverType, Type type, bool isHotfix);

    void End();
}