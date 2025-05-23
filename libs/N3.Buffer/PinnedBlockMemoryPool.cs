using System.Buffers;
using System.Collections.Concurrent;

namespace N3.Buffer
{
    public class PinnedBlockMemoryPool : MemoryPool<byte>
    {
        public static PinnedBlockMemoryPool Default = new PinnedBlockMemoryPool();

        public const int BlockSize = 4096;
        private readonly object _disposeSync = new();

        private readonly ConcurrentStack<MemoryBlock> _blocks = new();

        private bool _isDisposed;

        public override int MaxBufferSize => BlockSize;

        public void Return(MemoryBlock block)
        {
            if (_isDisposed)
                return;
            _blocks.Push(block);
        }

        public MemoryBlock RentBlock(int minBufferSize = -1)
        {
            if (!_blocks.TryPop(out var block))
            {
                block = new MemoryBlock(this, BlockSize);
            }
            return block;
        }

        public override IMemoryOwner<byte> Rent(int minBufferSize = -1) => RentBlock(minBufferSize);

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            lock (_disposeSync)
            {
                _isDisposed = true;

                if (disposing)
                {
                    // Discard blocks in pool
                    _blocks.Clear();
                }
            }
        }
    }
}