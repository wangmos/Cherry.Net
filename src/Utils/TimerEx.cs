using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cherry.Net.Utils
{
    public static class TimerEx
    {
        /// <summary>
        /// 开启一个计时器 回调返回true 继续执行 false 关闭计时器
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static async void StartTimerAsync(Func<bool> handle, int ms)
        {
            while (true)
            {
                await Task.Delay(ms);
                if (!handle()) break;
            }
        }

        /// <summary>
        /// 开启一个计时器 回调返回true 继续执行 false 关闭计时器
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static void StartTimer(Func<bool> handle, int ms)
        {
            Timer timer = null;
            timer = new Timer(o =>
            {
                if (handle()) timer.Change(ms, -1);
                else timer.Dispose();
            }, null, ms, -1);
        } 
        /// <summary>
        /// 一次性 延迟执行
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ms"></param>
        public static async void DelayAsync(Action handle, int ms)
        {
            await Task.Delay(ms);
            handle();
        }
        /// <summary>
        /// 一次性 延迟执行
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ms"></param>
        public static void Delay(Action handle, int ms)
        {
            Timer timer = null;
            timer = new Timer(o =>
            {
                handle();
                timer.Dispose();
            }, null, ms, -1);
        }
    }
}