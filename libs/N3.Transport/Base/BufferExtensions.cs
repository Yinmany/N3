using N3.Buffer;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;

namespace N3.Network;

public static class BufferExtensions
{
    public static short ReadInt16(this ReadOnlySequence<byte> self)
    {
        Span<byte> bytes = stackalloc byte[2];
        self.Slice(0, 2).CopyTo(bytes);
        return BinaryPrimitives.ReadInt16LittleEndian(bytes);
    }

    public static int ReadInt32(this ReadOnlySequence<byte> self)
    {
        Span<byte> bytes = stackalloc byte[4];
        self.Slice(0, 4).CopyTo(bytes);
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    public static long ReadInt64(this ReadOnlySequence<byte> self)
    {
        Span<byte> bytes = stackalloc byte[8];
        self.Slice(0, 8).CopyTo(bytes);
        return BinaryPrimitives.ReadInt64LittleEndian(bytes);
    }

    public static void WriteByteBuf(this PipeWriter output, ByteBuf byteBuf)
    {
        int bodyLen = (int)byteBuf.Length;
        while (bodyLen > 0)
        {
            Span<byte> sendBuffer = output.GetSpan(bodyLen);
            if (sendBuffer.Length > bodyLen)
                sendBuffer = sendBuffer[..bodyLen];
            byteBuf.ReadExactly(sendBuffer);
            output.Advance(sendBuffer.Length);
            bodyLen -= sendBuffer.Length;
        }
    }

    public static void WriteByFixedLengthField(this PipeWriter output, ByteBuf byteBuf)
    {
        // 先写入4字节长度
        int bodyLen = (int)byteBuf.Length;
        Span<byte> bodyLenSpan = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bodyLenSpan, bodyLen);
        output.Write(bodyLenSpan);

        while (bodyLen > 0)
        {
            Span<byte> sendBuffer = output.GetSpan(bodyLen);
            if (sendBuffer.Length > bodyLen)
                sendBuffer = sendBuffer[..bodyLen];
            byteBuf.ReadExactly(sendBuffer);
            output.Advance(sendBuffer.Length);
            bodyLen -= sendBuffer.Length;
        }
    }

    public static void CopyTo(this ReadOnlySequence<byte> buffer, ByteBuf data)
    {
        if (buffer.IsSingleSegment)
        {
            data.Write(buffer.FirstSpan);
        }
        else
        {
            // 多个
            var enumerator = buffer.GetEnumerator();
            enumerator.MoveNext();
            bool end;
            do
            {
                var currentBuffer = enumerator.Current;
                end = enumerator.MoveNext(); // 没有下一个，当前就是结束消息。
                data.Write(currentBuffer.Span);
            } while (!end);
        }
    }

    public static void CopyTo(this ReadOnlySequence<byte> buffer, ByteBuf data, Span<byte> maskKey)
    {
        if (buffer.IsSingleSegment)
        {
            CopyOneSpan(buffer.FirstSpan, data, maskKey);
        }
        else
        {
            // 多个
            var enumerator = buffer.GetEnumerator();
            enumerator.MoveNext();
            bool end;
            do
            {
                var currentBuffer = enumerator.Current;
                CopyOneSpan(currentBuffer.Span, data, maskKey);
                end = enumerator.MoveNext();
            } while (!end); // 没有下一个，当前就是结束消息。
        }

        void CopyOneSpan(ReadOnlySpan<byte> bytes, ByteBuf data, Span<byte> maskKey)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                data.WriteByte((byte)(bytes[i] ^ maskKey[i % 4]));
            }
        }
    }
}
