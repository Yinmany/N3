using System.Xml.Serialization;

namespace N3.GenTools;

[XmlRoot("conf")]
public class GenConfig
{
    [XmlElement("var")] public VarConfig[] Vars { get; set; }

    [XmlElement("proto")] public ProtoConfig[] Proto { get; set; }

    [XmlElement("enum")] public EnumConfig[] Enum { get; set; }

    [XmlElement("handler")] public HandlerConfig[] Handler { get; set; }
}

public class VarConfig
{
    [XmlAttribute("name")] public string Name { get; set; }
    [XmlAttribute("value")] public string Value { get; set; }
}

public class ProtoConfig
{
    [XmlAttribute("in")] public string In { get; set; }
    [XmlAttribute("out")] public string Out { get; set; }
    [XmlAttribute("tpl_base")] public string TplBase { get; set; }
    [XmlAttribute("tpl")] public string Tpl { get; set; }
    [XmlAttribute("msg_id")] public string MsgId { get; set; }
}

public class EnumConfig
{
    [XmlAttribute("in")] public string In { get; set; }
    [XmlAttribute("out")] public string Out { get; set; }
    [XmlAttribute("tpl_base")] public string TplBase { get; set; }
    [XmlAttribute("tpl")] public string Tpl { get; set; }
    [XmlAttribute("i18n")] public bool EnableI18n { get; set; }
}

public class HandlerGenConfig
{
    [XmlAttribute("type")]
    public int Type { get; set; }

    /// <summary>
    /// Id可以填多个具体Id: 100001,100002
    /// </summary>
    [XmlAttribute("id")]
    public string Id { get; set; }

    [XmlAttribute("prefix")]
    public string Prefix { get; set; }

    [XmlAttribute("out")]
    public string Out { get; set; }

    /// <summary>
    /// 排除的Id
    /// </summary>
    [XmlAttribute("exclude")]
    public string Exclude { get; set; }
    [XmlAttribute("ctx_type")] public string ContextType { get; set; }

    public int[] Ids { get; set; }

    /// <summary>
    /// 排除的Id
    /// </summary>
    public int[] ExcludeIds { get; set; }
}

public class HandlerConfig
{
    [XmlAttribute("tpl")] public string Tpl { get; set; }
    [XmlAttribute("msg_id")] public string MsgId { get; set; }
    [XmlAttribute("namespace")] public string Namespace { get; set; }
    [XmlAttribute("using")] public string Usings { get; set; }
    [XmlElement("gen")] public HandlerGenConfig[] GenConfig { get; set; }


    public string[] UsingArrary { get; set; }
}