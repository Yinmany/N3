using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace N3
{
    public interface ISObjectPoolNode<T> where T : class
    {
        ref T? NextNode { get; }
    }

    /// <summary>
    /// 对象池(无锁线程安全，单链表结构)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [StructLayout(LayoutKind.Auto)]
    public struct SObjectPool<T> where T : class, ISObjectPoolNode<T>
    {
        private int gate;
        private int size;
        private T? root;

        public int Size => this.size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T? result)
        {
            if (Interlocked.CompareExchange(ref this.gate, 1, 0) == 0)
            {
                T? tmpRoot = this.root;
                if (tmpRoot != null)
                {
                    ref var local = ref tmpRoot.NextNode;
                    this.root = local;
                    local = default;
                    --this.size;
                    result = tmpRoot;
                    Volatile.Write(ref this.gate, 0);
                    return true;
                }

                Volatile.Write(ref this.gate, 0);
            }

            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPush(T item)
        {
            if (Interlocked.CompareExchange(ref this.gate, 1, 0) == 0)
            {
                if (this.size < int.MaxValue)
                {
                    item.NextNode = this.root;
                    this.root = item;
                    ++this.size;
                    Volatile.Write(ref this.gate, 0);
                    return true;
                }

                Volatile.Write(ref this.gate, 0);
            }

            return false;
        }
    }
}