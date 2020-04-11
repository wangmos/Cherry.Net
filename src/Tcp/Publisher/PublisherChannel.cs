using System.Collections.Generic;

namespace Cherry.Net.Tcp.Publisher
{
    internal class PublisherChannel:TcpMsgChannel<PublisherChannel>
    {
        /// <summary>
        /// 订阅的消息集合
        /// </summary>
        public HashSet<string> MsgNames = new HashSet<string>();

        protected override void OnInitialized()
        {
            MsgNames.Clear();
        }

        protected override void OnClosed()
        {
            MsgNames.Clear();
        }
    }
}