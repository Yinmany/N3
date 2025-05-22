using System;

namespace N3Lib
{


    /// <summary>
    /// 分布式的id生成器(无锁线程安全，同进程有序)
    ///     实现原理: 让时间与序号组成一个long值，只用原子自增即可
    ///  1位符号位: 0
    /// 31位时间: 68年
    /// 12位机器码: 4096
    /// 20位序号: 1,048,575 (1秒百万)
    /// </summary>
    public readonly partial struct Did
    {
        /// <summary>
        /// 时间值(秒)
        /// </summary>
        public readonly int Time;

        /// <summary>
        /// 节点id
        /// </summary>
        public readonly ushort NodeId;

        /// <summary>
        /// 序号值
        /// </summary>
        public readonly int Seq;

        public Did(long id)
        {
            Time = (int)(id >> 32);

            NodeId = (ushort)(id >> SeqBits & MaxNodeId);

            Seq = (int)(id & MaxSeq);
        }

        public Did(int time, ushort nodeId, int seq)
        {
            if (seq > MaxSeq)
                throw new ArgumentException($"seq不能超过{MaxSeq}: {seq}");

            this.Time = time;
            this.NodeId = nodeId;
            this.Seq = seq;
        }

        public static implicit operator Did(long id)
        {
            return new Did(id);
        }

        public static implicit operator long(in Did id)
        {
            return (long)((ulong)id.Time << 32 | (ulong)id.NodeId << SeqBits | (uint)id.Seq);
        }
    }
}