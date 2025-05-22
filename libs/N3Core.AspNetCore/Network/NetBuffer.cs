using System.Buffers.Binary;
using Microsoft.IO;

namespace N3Core.AspNetCore;

public static class NetBuffer
{
    public const int BlockSize = 1024 * 4;
    public static readonly RecyclableMemoryStreamManager Default;

    static NetBuffer()
    {
        var options = new RecyclableMemoryStreamManager.Options
        {
            BlockSize = BlockSize, // 小型缓冲区(单个大小)
            LargeBufferMultiple = 1024 * 128, // 大缓存区倍数
            MaximumBufferSize = 1024 * 128 * 10, // 大缓存区分为10个格子: 倍数 * 1 | 倍数 * 2 ... 10(1.25M)
            MaximumLargePoolFreeBytes = 1024 * 128 * 10 * 4, // 超过4倍的最大缓冲区大小，就gc掉；不回收了
            MaximumSmallPoolFreeBytes = 1024 * 8 * 100,
            GenerateCallStacks = true, // 是否保存流分配的调用堆栈。这有助于调试。通常，在生产环境中通常不应打开它。

            // 是否可以立即将脏缓冲区返回到缓冲池。
            // 当对流调用 GetBuffer（） 并创建单个大型缓冲区时，如果启用此设置，则其他块将立即返回到缓冲池。
            // 请注意，启用此设置时，用户负责确保之前从随后修改的流中检索的任何缓冲区在修改后不会被使用（因为它可能不再有效）。
            AggressiveBufferReturn = false
        };

        Default = new RecyclableMemoryStreamManager(options);
    }

    public static RecyclableMemoryStream Rent()
    {
        return Default.GetStream();
    }

    public static int ReadInt32(this RecyclableMemoryStream self) => (int)self.ReadUInt32();

    public static uint ReadUInt32(this RecyclableMemoryStream self)
    {
        if (self.Length < 4) throw new IndexOutOfRangeException();

        Span<byte> tmp = stackalloc byte[4];
        _ = self.Read(tmp);
        return BinaryPrimitives.ReadUInt32LittleEndian(tmp);
    }

    public static long ReadInt64(this RecyclableMemoryStream self) => (long)self.ReadUInt64();

    public static ulong ReadUInt64(this RecyclableMemoryStream self)
    {
        if (self.Length < 8) throw new IndexOutOfRangeException();

        Span<byte> tmp = stackalloc byte[8];
        _ = self.Read(tmp);
        return BinaryPrimitives.ReadUInt64LittleEndian(tmp);
    }

    public static void WriteUInt32(this RecyclableMemoryStream self, uint value)
    {
        self.WriteInt32((int)value);
    }

    public static void WriteInt32(this RecyclableMemoryStream self, int value)
    {
        Span<byte> tmp = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(tmp, value);
        self.Write(tmp);
    }
}