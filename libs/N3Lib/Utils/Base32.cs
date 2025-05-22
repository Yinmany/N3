using System;
using System.Text;

namespace N3Lib
{
    public static class Base32
    {
        // 去掉I O 1 0 的默认编码表
        private const string Code = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        /// <summary>
        /// Base32编码
        /// </summary>
        /// <param name="bytes"></param>
        public static string Encode(ReadOnlySpan<byte> bytes)
        {
            // 偶数, 不足16字节补充0
            int len = bytes.Length;
            if (len % 2 != 0)
                len += 1;

            StringBuilder sb = new();

            uint buffer = 0;
            for (int i = 0; i < len; ++i)
            {
                uint tmp = i < bytes.Length ? bytes[i] : 0u;
                if (i != 0)
                {
                    tmp <<= 8;
                    buffer >>= 5;
                }
                else
                {
                    buffer |= tmp;
                }

                byte val = (byte)(buffer & 0x1f);
                sb.Append(Code[val]);
            }
            return sb.ToString();
        }
    }
}