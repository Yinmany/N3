using System;

namespace N3
{
    /// <summary>
    /// 礼包码生成器(10亿级)
    ///     8位(1索引+6ID+1签名)
    /// </summary>
    public class GiftCodeGenerator
    {
        // 去掉I O 1 0 的默认编码表
        private const string STR = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly char[][] _codingTable;

        // 密钥表
        private readonly byte[][] _keyTable;

        // 10亿个 1,073,741,823
        private int _id;

        private readonly Random _random;

        public GiftCodeGenerator(int id, int seed, byte num = 32)
        {
            if (id is < 0 or > 0x3FFFFFFF)
                throw new ArgumentException($"startId不能小于0，并且不能大于{0x3FFFFFFF}: 当前{id}", nameof(id));

            if (num is > 32 or < 1)
                throw new ArgumentException($"num最大只能为32，最小只能为1: 当前{num}", nameof(num));

            this._id = id;
            _random = new Random(seed);

            // 生成编码表
            _codingTable = new char[num][];
            _keyTable = new byte[num][];
            for (int i = 0; i < num; i++)
            {
                // 打乱编码表
                _codingTable[i] = STR.ToCharArray();
                _random.Shuffle(_codingTable[i]);

                // 随机生成密钥
                byte[] key = new byte[6];
                _random.NextBytes(key);
                _keyTable[i] = key;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buff">最少8位</param>
        /// <param name="offset"></param>
        public void Next(Span<byte> buff, int offset = 0)
        {
            // 索引值(编码表与签名表) 1个字符(A - 9)
            int idx = _random.Next(0, 32);

            char[] chars = _codingTable[idx % _codingTable.Length];
            byte[] key = _keyTable[idx % _keyTable.Length];

            int val = ++_id;
            int sum = 0;

            // 索引值
            buff[offset] = (byte)STR[(byte)idx];

            // 值6位
            for (byte i = 0; i < 6; i++)
            {
                int v = (val >> (i * 5)) & 0x1f;
                buff[i + offset + 1] = (byte)chars[v];

                // 加权求和，进行签名
                sum += v * key[i];
            }

            // 取余的签名值
            int sign = sum % 32;
            buff[offset + 7] = (byte)chars[sign];
        }

        public bool TryParse(string str, out int id)
        {
            id = 0;

            // 取出索引值
            char c = str[0];
            int idx = STR.IndexOf(c);
            if (idx == -1)
                return false;

            char[] chars = _codingTable[idx];
            byte[] key = _keyTable[idx];

            int sum = 0;
            for (int i = 0; i < 6; i++)
            {
                c = str[i + 1];
                int v = Array.IndexOf(chars, c);
                if (v == -1)
                    return false;
                id |= v << (i * 5);
                sum += v * key[i];
            }

            // 验证签名
            int sign = Array.IndexOf(chars, str[str.Length - 1]);
            if (sign != sum % 32)
                return false;
            return true;
        }
    }
}
