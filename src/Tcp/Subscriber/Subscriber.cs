using System;
using System.Collections.Concurrent;


namespace Cherry.Net.Tcp.Subscriber
{
    public class Subscriber
    {
        static Subscriber()
        {
            SubscriberChannel.Init(new NetConfig(){MaxPoolNum = 0});
        }

        /// <summary>
        /// 已经订阅的消息
        /// </summary>
        private readonly ConcurrentDictionary<string, Action<Subscriber,NetBuff>> _msg
            = new ConcurrentDictionary<string, Action<Subscriber, NetBuff>>();
         
        private readonly SubscriberChannel _channel = new SubscriberChannel();
        private readonly Connector _connector = new Connector();

        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected => !_channel.Closed;

        public Action OnConnected,OnLose;

        public Subscriber()
        {
            _channel.OnFilterMsg = OnFilterMsg;
            _channel.OnLose = OnLose;

            _connector.OnGetSocket = socket =>
            {
                _channel.Init(socket);
                OnConnected?.Invoke();
            };
        } 

        public void Connetct(string ip, int port)
        {
            _connector.Connect(ip, port);
        }

        private bool OnFilterMsg(SubscriberChannel channel, MsgPacket msg)
        { 
            if (msg.Cmd == "publish") //发布
            {
                var msgName = msg.Pop_Str();
                if (_msg.TryGetValue(msgName, out var act))
                {
                    var publisherMsg = new NetBuff(msg.Length - msg.ReadPos);
                    publisherMsg.Append(msg.Data, msg.ReadPos, msg.Length - msg.ReadPos);
                    act(this, publisherMsg);
                }
            }
            return false;
        }

        public void Subscribe(string msgName, Action<Subscriber, NetBuff> act)
        {
            if (act == null) return;
            _msg[msgName] = act;

            var msg = new MsgPacket("subscribe");
            msg.Push(msgName);
            _channel?.SendMsg(msg);
        }

        public void Publish(string name, NetBuff msg)
        {
            var origMsg = new MsgPacket("publish");
            origMsg.Push(name).Append(msg.Data, 0, msg.Length);
            _channel?.SendMsg(origMsg);
        }
    }
}