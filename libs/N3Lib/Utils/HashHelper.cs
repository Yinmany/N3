using System;
using System.Security.Cryptography;
using System.Text;

namespace N3Lib
{
    public static partial class HashHelper
    {
        /// <summary>
        /// md5哈希
        /// </summary>
        /// <param name="str"></param>
        /// <param name="isShort">是否返回16位hash；否则32位</param>
        /// <returns>返回hex字符串</returns>
        public static string Md5(string str, bool isShort = false)
        {
            return Md5(Encoding.ASCII.GetBytes(str), isShort);
        }

        public static string Md5(byte[] data, bool isShort = false)
        {
            MD5 md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(data);
            StringBuilder sb = new StringBuilder();
            int len = isShort ? 16 : bytes.Length;
            for (var i = 0; i < len; i++)
            {
                var t = bytes[i];
                sb.AppendFormat("{0:x2}", t);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取文本的HmacSha256的哈希值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string HMACSHA256(string key, string data)
        {
            var bytes = HMACSHA256Bytes(key, data);
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        public static byte[] HMACSHA256Bytes(string key, string data)
        {
            using HMACSHA256 h = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            byte[] hashValue = h.ComputeHash(Encoding.UTF8.GetBytes(data));
            return hashValue;
        }
    }
}