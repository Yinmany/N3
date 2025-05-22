namespace N3Core.GenTools;

/// <summary>
/// Proto文件解析
/// </summary>
public static class ProtoFile
{
    internal static readonly char[] splitChars = { ' ', '\t' };

    public static ProtoGenData Parse(string protoText)
    {
        ProtoGenData genData = new ProtoGenData();
        string lastLine = String.Empty;
        foreach (string line in protoText.Split('\n'))
        {
            string newline = line.Trim();
            if (newline == "")
                continue;

            ParseLine(genData, newline, lastLine);
            lastLine = newline;
        }

        return genData;
    }

    private static void ParseLine(ProtoGenData genData, string newline, string lastline)
    {
        // 命名空间引用
        if (newline.StartsWith("//@using"))
        {
            string tmp = newline.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
            genData.UsingList.Add(tmp);
            return;
        }

        // proto的选项
        if (newline.StartsWith("option csharp_namespace"))
        {
            // cs命名空间
            if ("csharp_namespace".IndexOf(newline, StringComparison.Ordinal) != 0)
            {
                // "Namespace";
                string ns = newline.Split('=')[1];
                ns = ns.Remove(ns.Length - 1).Replace("\"", "");
                genData.Namespace = ns.Trim();
                return;
            }
        }

        // 开始解析消息
        if (newline.StartsWith("message"))
        {
            ProtoMessage protoMessage = new ProtoMessage(newline);
            genData.Push(protoMessage);

            if (lastline.StartsWith("//")) // 行注释
            {
                protoMessage.Comment = lastline.Replace("//", "").Trim();
            }

            return;
        }

        // 进入消息解析
        if (genData.TryPeek() is ProtoMessage messageDefine)
        {
            if (newline == "{")
                return;

            if (newline == "}")
            {
                genData.Pop(); // 弹出自己

                if (genData.TryPeek() is ProtoElement elementDefine)
                {
                    elementDefine.AddChild(messageDefine);
                }
                else
                {
                    genData.Elems.Add(messageDefine);
                }

                return;
            }

            string tmpLine = newline.Trim();

            if (tmpLine.StartsWith("option")) // 扩展选项
            {
                messageDefine.AddChild(new ProtoMessageOption(tmpLine));
                return;
            }

            if (tmpLine.StartsWith("//"))
            {
                messageDefine.AddChild(new ProtoComment { Comment = newline, TabCount = 2 });
                return;
            }

            if (TryParseEnum(genData, newline))
                return;

            messageDefine.AddChild(new ProtoMessageField(newline));
            return;
        }

        // 注释
        if (newline.StartsWith("//"))
        {
            genData.Elems.Add(new ProtoComment { Comment = newline, TabCount = 1 });
            return;
        }

        TryParseEnum(genData, newline);
    }

    private static bool TryParseEnum(ProtoGenData genData, string newline)
    {
        // 开始解析枚举
        if (newline.StartsWith("enum"))
        {
            genData.Push(new ProtoEnum(newline));
            return true;
        }

        if (genData.TryPeek() is ProtoEnum protoElementDefine)
        {
            if (newline == "{")
                return true;

            if (newline == "}")
            {
                genData.Pop(); // 弹出自己

                if (genData.TryPeek() is ProtoElement elementDefine)
                {
                    elementDefine.AddChild(protoElementDefine);
                }
                else
                {
                    genData.Elems.Add(protoElementDefine);
                }

                return true;
            }

            protoElementDefine.AddChild(new ProtoEnumField(newline));
            return true;
        }

        return false;
    }
}