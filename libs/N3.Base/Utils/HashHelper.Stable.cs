using System;

namespace N3
{
    public static partial class HashHelper
    {
        /// <summary>
        /// 稳定的Hash
        /// </summary>
        public static class StableHash
        {
            public static int Compute(string text)
            {
                // 不检测溢出 
                unchecked
                {
                    int hash = 23; // 一个质数 17 23 31 503
                    foreach (var c in text)
                        hash = hash * 31 + c;
                    return hash;
                }
            }

            // public static ushort ComputeInt16(Type type)
            // {
            //     return (ushort)(Compute(type.FullName!) & 0xffff);
            // }

            public static int Compute(Type type) => Compute(type.FullName!);

            public static int Combine(int a, int b)
            {
                // 不检测溢出 
                unchecked
                {
                    int hash = 23; // 一个质数 17 23 31 503
                    hash = hash * 31 + a;
                    hash = hash * 31 + b;
                    return hash;
                }
            }

            public static int Combine(int a, int b, int c)
            {
                // 不检测溢出 
                unchecked
                {
                    int hash = 23; // 一个质数 17 23 31 503
                    hash = hash * 31 + a;
                    hash = hash * 31 + b;
                    hash = hash * 31 + c;
                    return hash;
                }
            }
        }
    }
}