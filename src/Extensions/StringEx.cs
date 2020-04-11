using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Cherry.Net.Extensions
{
    public static class StringEx
    { 
        public static string GetSubStr(this string str, string startStr = null, string endStr = null,
            int startI = 0)
        {
            if (!string.IsNullOrEmpty(startStr))
            {
                var i = str.IndexOf(startStr, startI, StringComparison.Ordinal);
                if (i == -1) return null;

                startI = i + startStr.Length;
            }

            var endI = string.IsNullOrEmpty(endStr)
                ? str.Length
                : str.IndexOf(endStr, startI, StringComparison.Ordinal);

            return startI < endI ? str.Substring(startI, endI - startI) : null;
        }


        /// <summary>
        /// 到字节
        /// </summary>
        /// <param name="src"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToBytes(this string src, Encoding en = null)
        {
            return (en ?? Encoding.UTF8).GetBytes(src);
        }

        /// <summary>
        /// 转化十六进制形式文本到字节
        /// </summary>
        /// <param name="hexStr"></param>
        /// <returns></returns>
        public static byte[] ToHexBytes(this string hexStr)
        {  
            var strs = hexStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var ls = new List<byte>(hexStr.Length / 2 + 1);
            foreach (var str in strs)
            {
                var s = str.Length % 2 == 1 ? ("0" + str) : str;
                for (var j = 0; j < s.Length; j += 2)
                    ls.Add(Convert.ToByte(s.Substring(j, 2), 16)); 
            }
            return ls.ToArray();
        }

        /// <summary>
        /// 删除最后的字符
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder RemoveLast(this StringBuilder sb, int len = 1)
        {
            return sb.Length < len ? sb.Clear() : sb.Remove(sb.Length - len, len);
        }

        /// <summary>
        /// 删除最后的字符
        /// </summary>
        /// <param name="str"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RemoveLast(this string str, int len = 1)
        {
            return str.Length < len ? "" : str.Remove(str.Length - len, len);
        } 


        /// <summary>
        /// 分割字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="sep"></param>
        /// <param name="opt"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] Split(this string str, string sep,
            StringSplitOptions opt = StringSplitOptions.None)
        {
            return str.Split(new[] { sep }, opt);
        }


        #region regex

        /// <summary>
        /// 去掉所有空白字符
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string TrimAll(this string val) => Regex.Replace(val, @"\s+", "", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);


        /// <summary>
        /// 占位符$1
        /// </summary>
        /// <param name="val"></param>
        /// <param name="pattern"></param>
        /// <param name="replaceStr"></param>
        /// <returns></returns>
        public static string ReplaceReg(this string val, string pattern, string replaceStr) => Regex.Replace(val, pattern, replaceStr,
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

          
        /// <summary>
        /// 是否匹配
        /// </summary>
        /// <param name="val"></param>
        /// <param name="pattern"></param>
        /// <param name="ops"></param>
        /// <returns></returns>
        public static bool IsMatch(this string val, string pattern, RegexOptions ops = RegexOptions.None)
            => Regex.IsMatch(val, pattern, ops == RegexOptions.None ? (RegexOptions.Compiled | RegexOptions.IgnoreCase)
                : (RegexOptions.Compiled | RegexOptions.IgnoreCase | ops));

        /// <summary>
        /// 匹配
        /// </summary>
        /// <param name="val"></param>
        /// <param name="pattern"></param>
        /// <param name="ops"></param>
        /// <returns></returns>
        public static Match Match(this string val, string pattern, RegexOptions ops = RegexOptions.None)
            => Regex.Match(val, pattern, ops == RegexOptions.None ? (RegexOptions.Compiled | RegexOptions.IgnoreCase)
                : (RegexOptions.Compiled | RegexOptions.IgnoreCase | ops));

        /// <summary>
        /// 匹配一个
        /// </summary>
        /// <param name="val"></param>
        /// <param name="pattern"></param>
        /// <param name="ops"></param>
        /// <returns></returns>
        public static string MatchOne(this string val, string pattern, RegexOptions ops = RegexOptions.None)
        {
            var m = val.Match(pattern, ops);
            if (m?.Groups.Count == 2)
            {
                return m.Groups[1].Value;
            }
            return null;
        }
        /// <summary>
        /// 多个匹配
        /// </summary>
        /// <param name="val"></param>
        /// <param name="pattern"></param>
        /// <param name="ops"></param>
        /// <returns></returns>
        public static MatchCollection Matches(this string val, string pattern, RegexOptions ops = RegexOptions.None)
            => Regex.Matches(val, pattern, ops == RegexOptions.None ? (RegexOptions.Compiled | RegexOptions.IgnoreCase)
                : (RegexOptions.Compiled | RegexOptions.IgnoreCase | ops));

        #endregion

        #region string to 数字

        public static uint ToUInt(this string s) => uint.TryParse(s, out var i) ? i : 0;

        public static int ToInt(this string s) => int.TryParse(s, out var i) ? i : 0;

        public static ushort ToUShort(this string s) => ushort.TryParse(s, out var i) ? i : (ushort)0;

        public static short ToShort(this string s) => short.TryParse(s, out var i) ? i : (short)0;

        public static ulong ToULong(this string s) => ulong.TryParse(s, out var i) ? i : 0;

        public static long ToLong(this string s) => long.TryParse(s, out var i) ? i : 0;

        public static double ToDouble(this string s) => double.TryParse(s, out var i) ? i : 0;

        public static float ToFloat(this string s) => float.TryParse(s, out var i) ? i : 0;

        public static byte ToByte(this string s) => byte.TryParse(s, out var i) ? i : (byte)0;

        public static sbyte ToSByte(this string s) => sbyte.TryParse(s, out var i) ? i : (sbyte)0;

        public static bool ToBool(this string s) => bool.TryParse(s, out var i) && i;

        #endregion
    }
}