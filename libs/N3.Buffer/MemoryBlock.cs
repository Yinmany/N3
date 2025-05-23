using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;

namespace N3.Buffer
{
    public class MemoryBlock : IMemoryOwner<byte>
    {
        private readonly PinnedBlockMemoryPool _pool;
        public Memory<byte> Memory { get; }

        public byte[] Bytes { get; }

        public volatile MemoryBlock? Next;

        public int Id { get; }

        private static int IdGen;

        public MemoryBlock(PinnedBlockMemoryPool pool, int length)
        {
            _pool = pool;

            Id = Interlocked.Increment(ref IdGen);

#if NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
            byte[] bytes = new byte[length];
            GCHandle gCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned); // 防止gc移动这个数组
            Memory = bytes;
#else
        var bytes = GC.AllocateUninitializedArray<byte>(length, pinned: true);
        Memory = MemoryMarshal.CreateFromPinnedArray(bytes, 0, bytes.Length);
#endif
            Bytes = bytes;
        }

        public void Dispose()
        {
            Next = null;
            _pool.Return(this);
        }
    }
}