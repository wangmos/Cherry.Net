using System;

namespace Cherry.Net.Extensions
{
    public static class IntEx
    {
        /// <summary>
        /// 数据的大小文本表示
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToSize(this long val)
        {
            if (val > 1024 * 1024 * 1024)
            {
                return (val * 1.0 / (1024 * 1024 * 1024)).ToString("0.00Gb");
            }
            return val > 1024 * 1024 ? (val * 1.0 / (1024 * 1024)).ToString("0.00Mb") : (val * 1.0 / 1024).ToString("0.00Kb");
        }

        /// <summary>
        /// 数据的大小文本表示
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToSize(this int val) => ToSize((long)val);

        /// <summary>
        /// 向上对齐
        /// </summary>
        /// <param name="val"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static int UpTo(this int val, int num) => ((val - 1) / num + 1) * num;

        /// <summary>
        /// 向上舍入 5.5=> 6
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int Ceil(this double val) => (int)Math.Ceiling(val);

        /// <summary>
        /// 向下舍入 5.5=> 5
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int Floor(this double val) => (int)Math.Floor(val);

    }
}