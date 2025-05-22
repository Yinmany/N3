using System;
using System.Threading;

namespace N3Lib
{
    public partial struct Did
    {
        public const byte SeqBits = 20;
        public const int MaxSeq = 0xFFFFF;
        public const ushort MaxNodeId = 0xFFF;

        private static ulong _value;
        private static Action<int>? _onLeaseTime;

        public static ushort LocalNodeId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeId">节点id</param>
        /// <param name="onLeaseTime">借用时间的回调</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void Init(ushort nodeId, Action<int>? onLeaseTime = null)
        {
            if (_value != 0)
                throw new InvalidOperationException("请不要重复初始化.");

            if (nodeId > MaxNodeId)
                throw new ArgumentException($"nodeId不能超过{MaxNodeId}: {nodeId}");

            LocalNodeId = nodeId;

            int s = STime.NowSeconds;
            _value = (ulong)s << SeqBits;

            _onLeaseTime = onLeaseTime;
        }

        /// <summary>
        /// 唯一,单进程有序(因为时间值并不是取当前的,而是从初始化时，一直往后自增的)
        /// </summary>
        /// <returns></returns>
        public static long Next()
        {
            CheckInitThrow();

#if !NET6_0_OR_GREATER
            long tmpValue = (long)_value;
            ulong val = (ulong)Interlocked.Increment(ref tmpValue);
#else
            ulong val = (ulong)Interlocked.Increment(ref _value);
#endif
            int seq = (int)val & MaxSeq;
            int time = (int)(val >> SeqBits) & 0x7FFFFFFF;
            long id = new Did(time, LocalNodeId, seq);

            // 超过当前时间10s: 向未来借用了30s就要输出日志
            int leaseTime = time - STime.NowSeconds;
            if (leaseTime > 0 && id == 0 && leaseTime % 30 == 0)
            {
                _onLeaseTime?.Invoke(time);
            }

            return id;
        }

        private static void CheckInitThrow()
        {
            if (Volatile.Read(ref _value) == 0)
                throw new InvalidOperationException("请初始化后使用.");
        }

        /// <summary>
        /// 生成一个指定Id的Did
        ///     同Did是一样的, 只是没有了time而且id位只有占16位了
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nodeId">节点id</param>
        /// <returns></returns>
        public static Did Make(ushort id, ushort? nodeId = null)
        {
            CheckInitThrow();

            if (nodeId > MaxNodeId)
                throw new ArgumentException($"nodeId不能超过{MaxNodeId}: {nodeId}");

            ushort tmpNodeId = nodeId ?? LocalNodeId;
            return new Did(0, tmpNodeId, id);
        }
    }
}