namespace Cherry.Net
{
    public class NetConfig
    {
        /// <summary>
        /// 对象池缓存数量
        /// </summary>
        public int MaxPoolNum = 5000;

        /// <summary>
        /// 数据缓存池大小 推荐8192(默认)
        /// </summary>
        public ushort BufferSize = 8192;
    }
}