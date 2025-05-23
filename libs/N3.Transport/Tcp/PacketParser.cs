using N3.Buffer;
using System.Buffers;
using System.Buffers.Binary;

namespace N3.Network;

public interface IPacketParser
{
    bool TryParse(ref ReadOnlySequence<byte> buffer, out ByteBuf data);
}

/// <summary>
/// 固定长度字段解码器
/// </summary>
public class FixedLengthFieldDecoder : IPacketParser
{
    public static FixedLengthFieldDecoder Default { get; } = new FixedLengthFieldDecoder();

    public bool TryParse(ref ReadOnlySequence<byte> buffer, out ByteBuf data)
    {
        data = default;
        if (buffer.Length < 5) // 最少要5个
            return false;

        Span<byte> tmpSpan = stackalloc byte[4];
        buffer.Slice(0, 4).CopyTo(tmpSpan);
        uint bodyLen = BinaryPrimitives.ReadUInt32LittleEndian(tmpSpan);
        if (buffer.Length - 4 < bodyLen) // 数据不够
            return false;

        var bodyBuffer = buffer.Slice(4, bodyLen);
        buffer = buffer.Slice(bodyLen + 4);

        data = ByteBuf.Rent();
        bodyBuffer.CopyTo(data);
        return true;
    }
}