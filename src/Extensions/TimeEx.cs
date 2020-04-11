using System;

namespace Cherry.Net.Extensions
{
    /// <summary>
    ///     时间拓展
    /// </summary>
    public static class TimeEx
    {
        /// <summary>
        ///     格式化时间间隔 @"dd\.hh\:mm\:ss\:ff" ff毫秒
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="fmt"></param>
        /// <returns></returns>
        public static string ToFormatString(this TimeSpan ts, string fmt = @"dd\.hh\:mm\:ss")
        {
            return ts.ToString(fmt);
        }

        /// <summary>
        ///     格式化时间 @"yyyy-MM-dd HH:mm:ss"
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="fmt"></param>
        /// <returns></returns>
        public static string ToFormatString(this DateTime ds, string fmt = "yyyy-MM-dd HH:mm:ss")
        {
            return ds.ToString(fmt);
        }

        /// <summary>
        ///     格式化日期
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="fmt"></param>
        /// <returns></returns>
        public static string ToDateString(this DateTime ds, string fmt = "yyyy-MM-dd")
        {
            return ds.ToString(fmt);
        }


        /// <summary>
        ///     取得时间戳 毫秒
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static double ToTimestamp(this DateTime dateTime)
        {
            return (dateTime.ToUniversalTime() - DateTime.Parse("1970-1-01")).TotalMilliseconds;
        }


        /// <summary>
        ///     取得当前时间戳 毫秒
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long ToTimestampLong(this DateTime dateTime)
        {
            return (long)ToTimestamp(dateTime);
        }


        /// <summary>
        ///     取得当前时间戳 秒
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static int ToTimestampInt(this DateTime dateTime)
        {
            return (int)(ToTimestamp(dateTime) / 1000);
        }

        /// <summary>
        ///     时间戳到时间 毫秒
        /// </summary>
        /// <param name="millSec"></param>
        /// <returns></returns>
        public static DateTime ToDateTimeMilSec(double millSec)
        {
            return DateTime.Parse("1970-1-01").AddMilliseconds(millSec).ToLocalTime();
        }

        /// <summary>
        ///     时间戳到时间 秒
        /// </summary>
        /// <param name="sec"></param>
        /// <returns></returns>
        public static DateTime ToDateTimeSec(int sec)
        {
            return DateTime.Parse("1970-1-01").AddSeconds(sec).ToLocalTime();
        }

        /// <summary>
        ///     相对于今天凌晨的秒数
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static int TodaySecs(this DateTime dateTime)
        {
            return (int)(dateTime - DateTime.Today).TotalSeconds;
        }

        /// <summary>
        ///     相对于当天凌晨的秒数
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static int DaySecs(this DateTime dateTime)
        {
            return (int)(dateTime - dateTime.Date).TotalSeconds;
        }  
    }
}