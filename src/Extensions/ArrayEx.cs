using System;
using System.Text;

namespace Cherry.Net.Extensions
{
    public static class ArrayEx
    {  
        /// <summary>
        /// 查找多个数据的开始位置
        /// </summary>
        /// <typeparam name="T">任意类型</typeparam>
        /// <param name="src">数组</param>
        /// <param name="data">目标数据</param>
        /// <param name="index">开始位置</param>
        /// <param name="len">长度</param>
        /// <returns></returns>
        public static int Find<T>(this T[] src, T[] data, int index = 0, int len = 0)
        {
            if (len == 0) len = src.Length - index;
            if (data.Length > len) return -1;
            var lastIndex = index + len - data.Length;

            for (; index <= lastIndex; ++index)
            {
                var find = true;
                for (var x = 0; x < data.Length; ++x)
                {
                    if (!data[x].Equals(src[index + x]))
                    {
                        find = false;
                        break;
                    }
                }
                if (find)
                {
                    return index;
                }
            }
            return -1;
        } 

        /// <summary>
        /// 取得范围
        /// </summary>
        /// <param name="src"></param>
        /// <param name="index"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static T[] GetRange<T>(this T[] src, int index, int len)
        {
            var bs = new T[len];
            Array.Copy(src, index, bs, 0, len);
            return bs;
        }

        /// <summary>
        ///     十六进制字符串
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="index">开始位置</param>
        /// <param name="len">长度</param>
        /// <param name="sep">分隔符</param>
        /// <returns></returns>
        public static string ToHexString(this byte[] array, string sep = "", int index = 0, int len = 0)
        {
            if (len == 0) len = array.Length - index;
            return BitConverter.ToString(array, index, len).Replace("-", sep);
        }

        /// <summary>
        ///     获取字符串
        /// </summary>
        /// <param name="src">数组</param>
        /// <param name="index"></param>
        /// <param name="len"></param>
        /// <param name="en">编码</param>
        /// <returns></returns>
        public static string ToEncodingString(this byte[] src, int index = 0, int len = 0, Encoding en = null)
        {
            if (len == 0) len = src.Length - index;
            return (en ?? Encoding.UTF8).GetString(src, index, len);
        }

    }
}