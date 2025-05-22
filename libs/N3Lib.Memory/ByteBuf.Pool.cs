using System.Collections.Concurrent;

namespace N3Lib.Buffer;

public partial class ByteBuf
{
    private static readonly ConcurrentStack<ByteBuf> Stack = new();

    private static MemoryBlock RentBuffer()
    {
        return PinnedBlockMemoryPool.Default.RentBlock();
    }

    public static ByteBuf Rent()
    {
        if (!Stack.TryPop(out ByteBuf? buf))
        {
            buf = new ByteBuf();
        }
        else
        {
            buf._readNode = buf._writeNode = RentBuffer();
        }
        buf.Disposed = false;
        return buf;
    }

    private static void Return(ByteBuf buf)
    {
        Stack.Push(buf);
    }
}