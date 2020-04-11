using System;
using Cherry.Net.Utils;

namespace Cherry.Net.Tcp.Publisher
{
    public class Publisher
    {
        static Publisher()
        {
            PublisherChannel.Init(new NetConfig(){MaxPoolNum = 20});
        }

        /// <summary>
        /// 每个消息订阅的频道id
        /// </summary>
        private readonly ClassifyMgr<string,uint> _subMsg = new ClassifyMgr<string,uint>();

        public Action OnStart;
        public Action<string> OnStop;

        public Publisher()
        {
            PublisherChannel.OnGetChannel = channel =>
            {
                channel.OnFilterMsg = OnFilterMsg;
            };
            PublisherChannel.OnLoseChannel = OnCloseChannel;

            PublisherChannel.Listener.OnSucc = (listener, i) => OnStart?.Invoke();
            PublisherChannel.Listener.OnFail = (listener, i, err) => OnStop?.Invoke(err);
        }

        public void Start(int port)
        {
            PublisherChannel.Listener.Start(port);
        }

        private void OnCloseChannel(PublisherChannel channel)
        {  
            //删除订阅的消息
            lock (channel.MsgNames)
            {
                foreach (var name in channel.MsgNames)
                {
                    _subMsg.Del(name, channel.Id);
                }
            }
        }

        /// <summary>
        /// 频道过滤消息
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        private bool OnFilterMsg(PublisherChannel channel, MsgPacket msg)
        {
            //订阅
            if (msg.Cmd == "subscribe")
            {
                var msgName = msg.Pop_Str();
                _subMsg.Add(msgName, channel.Id, true);
            }
            else if(msg.Cmd == "publish") //发布
            {
                var msgName = msg.Pop_Str();
                if(_subMsg.TryGetValue(msgName, out var ls))
                {
                    lock (ls)
                    {
                        foreach (var id in ls)
                        {
                            if(id == channel.Id)continue;
                            PublisherChannel.GetChannel(id)?.SendMsg(msg.Clone());
                        }
                    }
                }
            }
            return false;
        }

        public void Publish(string name, NetBuff msg)
        {
            if (_subMsg.TryGetValue(name, out var ls))
            {
                var origMsg = new MsgPacket("publish");
                origMsg.Push(name).Append(msg.Data,0,msg.Length);

                lock (ls)
                {
                    foreach (var id in ls)
                    {
                        PublisherChannel.GetChannel(id)?.SendMsg(origMsg.Clone());
                    }
                }
            }
        }
    }
}