using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Cherry.Net.Extensions
{
    public static class HttpEx
    {/// <summary>
        /// url编码
        /// </summary>
        /// <param name="str"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string UrlEncode(this string str, Encoding encoding = null) => HttpUtility.UrlEncode(str, encoding ?? Encoding.UTF8);

        /// <summary>
        /// url解码  \u9876\u9876\u9876\u9876 也可以解
        /// </summary>
        /// <param name="str"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string UrlDecode(this string str, Encoding encoding = null) => HttpUtility.UrlDecode(str, encoding ?? Encoding.UTF8);

        /// <summary>
        /// Html编码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HtmlEncode(this string str) => HttpUtility.HtmlEncode(str);

        /// <summary>
        /// html解码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HtmlDecode(this string str) => HttpUtility.HtmlDecode(str);

        /// <summary>
        /// Js编码 顶顶顶顶 => \u9876\u9876\u9876\u9876
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string JsEncode(this string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str.ToCharArray())
            {
                if (c >= 0x4e00 && c <= 0x9fa5)
                    sb.Append($"\\u{((int)c):x4}");
                else sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Js解码 \u9876\u9876\u9876\u9876 => 顶顶顶顶
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string JsDecode(this string str) => Regex.Unescape(str);
    }
}