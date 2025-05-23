using System.Net;
using System.Xml;

namespace N3;

internal class NodeConfig
{
    public ushort Id { get; internal set; }
    public IPEndPoint IPEndPoint { get; internal set; }
}

public class ServerConfig
{
    public ushort Id { get; }
    public string Name { get; }
    public ushort Type { get; }

    /// <summary>
    /// 所在节点id
    /// </summary>
    public ushort NodeId { get; }

    public IReadOnlyDictionary<string, string> Kv { get; }

    private ServerConfig(ushort id, string name, ushort type, ushort nodeId, Dictionary<string, string> kv)
    {
        Id = id;
        Name = name;
        Type = type;
        NodeId = nodeId;
        Kv = kv;
    }

    /// <summary>
    /// 当前节点id
    /// </summary>
    public static ushort LocalNodeId => Did.LocalNodeId;

    public static IReadOnlyList<ServerConfig> All => Configs;
    private static readonly List<ServerConfig> Configs = new();
    private static readonly Dictionary<ushort, NodeConfig> NodeList = new();
    private static readonly Dictionary<ushort, List<ServerConfig>> NodeServerList = new();

    /// <summary>
    /// 全局通用的Kv配置
    /// </summary>
    public static IReadOnlyDictionary<string, string> GlobalKv { get; private set; }

    /// <summary>
    /// 获取节点下的所有服务器
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    public static IReadOnlyList<ServerConfig>? GetAllByNodeId(ushort nodeId)
    {
        NodeServerList.TryGetValue(nodeId, out var list);
        return list;
    }

    /// <summary>
    /// 根据节点id和服务器id获取服务器配置
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="serverId"></param>
    /// <returns></returns>
    public static ServerConfig? GetConfig(ushort nodeId, ushort serverId)
    {
        return GetAllByNodeId(nodeId)?.FirstOrDefault(f => f.Id == serverId);
    }

    /// <summary>
    /// 获取节点的ip
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    public static IPEndPoint? GetNodeIp(ushort nodeId)
    {
        return NodeList.GetValueOrDefault(nodeId)?.IPEndPoint;
    }

    /// <summary>
    /// 根据服务器类型查找一个服务器配置
    /// </summary>
    /// <param name="serverType"></param>
    /// <returns></returns>
    public static ServerConfig? FindOneByServerType(ushort serverType)
    {
        return All.FirstOrDefault(f => f.Type == serverType);
    }

    public static void Init(string path, ushort nodeId)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(path);
        XmlElement? root = doc.DocumentElement;
        if (root is null)
            throw new Exception("xml格式错误");

        XmlElement? configNode = root["config"];
        GlobalKv = ParseKv(configNode);

        // 节点配置
        foreach (XmlNode item in root.ChildNodes)
        {
            if (item is not XmlElement e)
                continue;
            if (item.Name != "node")
                continue;

            var idAttr1 = e.Attributes["id"];
            if (idAttr1 is null)
                throw new Exception("node.id属性必须填写");
            var listenAttr = e.Attributes["listen"];
            if (listenAttr is null)
                throw new Exception("node.listen属性必须填写");

            NodeConfig nodeConfig = new NodeConfig();
            nodeConfig.Id = ushort.Parse(idAttr1.Value);
            nodeConfig.IPEndPoint = IPEndPoint.Parse(listenAttr.Value);
            //nodeConfig.Kv = ParseKv(item);

            // 配置中的节点id
            NodeList.Add(nodeConfig.Id, nodeConfig);
            Parse(e, nodeConfig.Id);
        }

        Did.Init(nodeId);
    }

    private static void Parse(XmlElement e, ushort nodeIdCfg)
    {
        foreach (XmlNode item2 in e)
        {
            if (item2 is not XmlElement e1)
                continue;

            var idAttr = e1.Attributes["id"];
            if (idAttr is null)
                throw new Exception("id属性必须填写");
            var nameAttr = e1.Attributes["name"];
            if (nameAttr is null)
                throw new Exception("name属性必须填写");
            var typeAttr = e1.Attributes["type"];
            if (typeAttr is null)
                throw new Exception("type属性必须填写");

            ushort id = ushort.Parse(idAttr.Value);
            string name = nameAttr.Value;
            ushort type = ushort.Parse(typeAttr.Value);

            var kv = ParseKv(item2);

            ServerConfig c = new ServerConfig(id, name, type, nodeIdCfg, kv);
            Configs.Add(c);

            if (!NodeServerList.TryGetValue(nodeIdCfg, out var list))
            {
                list = new List<ServerConfig>();
                NodeServerList.Add(nodeIdCfg, list);
            }

            list.Add(c);
        }
    }

    private static Dictionary<string, string> ParseKv(XmlNode? item2)
    {
        Dictionary<string, string> kv = new Dictionary<string, string>();
        if (item2 is null)
            return kv;

        foreach (XmlNode item3 in item2)
        {
            if (item3 is not XmlElement e2)
                continue;
            if (item3.Name != "key")
                continue;

            var nameAttr2 = e2.Attributes["name"];
            if (nameAttr2 is null)
                throw new Exception($"{item2.Name}.key.name属性必须填写");

            var valueAttr2 = e2.Attributes["value"];
            if (valueAttr2 is null)
                throw new Exception($"{item2.Name}.key.value属性必须填写");
            kv.Add(nameAttr2.Value.Trim(), valueAttr2.Value);
        }

        return kv;
    }
}