namespace N3Core.GenTools;

public record struct EnumItem(string Name, int Value, string Comment, string[] Localization);

public class EnumData
{
    public string Namespace;
    public string Name;
    public EnumItem[] Items;
    public string[] Language;
    public bool IsI18n; // 是否生成多语言
}