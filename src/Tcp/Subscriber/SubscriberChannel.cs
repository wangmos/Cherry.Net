using System;

namespace Cherry.Net.Tcp.Subscriber
{
    internal class SubscriberChannel:TcpMsgChannel<SubscriberChannel>
    {
        public Action OnLose;
        protected override void OnClosed()
        {
            base.OnClosed();
            OnLose?.Invoke();
        }
    }
}