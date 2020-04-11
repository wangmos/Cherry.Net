using System;
using System.Collections.Concurrent;

namespace Cherry.Net.Tcp
{
    public abstract class TcpMsgChannel<T>:TcpChannel<T> where T : TcpMsgChannel<T>,new()
    {
        public delegate void MsgHandle(T channel, MsgPacket msg);

        public delegate bool FilterHandle(T channel, MsgPacket msg);

        private static readonly ConcurrentDictionary<string, MsgHandle> CmdHandle
            = new ConcurrentDictionary<string, MsgHandle>();

        public static void SetCmdHandle(string cmd, MsgHandle handle)
        {
            if (handle == null) return;
            CmdHandle[cmd] = handle;
        }


        /// <summary>
        /// 缓存消息
        /// </summary>
        private MsgPacket _msg;

        /// <summary>
        /// 过滤信息包 返回true则中断派发
        /// </summary>
        public FilterHandle OnFilterMsg;

        /// <summary>
        /// 发送数据之前
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="len"></param>
        protected virtual void OnSendMsg(byte[] data, int index, int len)
        {

        }

        /// <summary>
        /// 解出数据之前
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="len"></param>
        protected virtual void OnReceiveMsg(byte[] data, int index, int len)
        {

        }

        protected override void OnInitialized()
        {
            _msg = null;
        }

        protected override void OnClosed()
        {
            _msg = null;
        }


        public void SendMsg(MsgPacket msg)
        {
            var bufferSize = Config.BufferSize;

            //确保在分包的时候 不会错乱
            lock (CmdHandle)
            {
                if (msg.Length <= bufferSize)
                {
                    msg.Packet(true, 0);
                    OnSendMsg(msg.Data, 2, msg.Length - 2);
                    Send(msg.Data, 0, msg.Length);
                    return;
                }

                //分包发送
                var sendIndex = msg.ContentPos;

                while (sendIndex < msg.Length)
                {
                    var sendLen = msg.Length - sendIndex; //需要发送的字节数
                    var i = bufferSize - msg.ContentPos; //能够发送的最大字节数
                    var end = sendLen <= i;

                    if (!end) sendLen = i - 4; //不是最后一个包 留出4个字节存放整个包的长度

                    var sendMsg = new MsgPacket(sendLen + msg.ContentPos)
                    {
                        Cmd = msg.Cmd,
                    };
                    sendMsg.Push(msg.Cmd);
                    if (!end) sendMsg.Push(msg.Length); //非最后一个包

                    sendMsg.Append(msg.Data, sendIndex, sendLen);
                    sendMsg.Packet(end, 0);
                    OnSendMsg(sendMsg.Data, 2, sendMsg.Length - 2);
                    Send(sendMsg.Data, 0, sendMsg.Length);
                    sendIndex += sendLen;
                }
            }
        }

        protected override void OnReceiveData(NetBuff buff)
        {
            while (buff.ReadPos + 2 <= buff.Length)
            {
                var pLen = buff.Pop_UShort();

                if (pLen < MsgPacket.FreeNum || pLen > buff.Capacity)
                {
                    OnError($"Address:{RemoteAddress} Packet Len Error:[{pLen}]");
                    Close();
                    return;
                }

                var packetPos = buff.ReadPos - 2;
                if (packetPos + pLen > buff.Length)
                {
                    buff.ReadPos = packetPos;
                    break;
                }


                OnReceiveMsg(buff.Data, buff.ReadPos, pLen - 2);
                var code = buff.Pop_UShort();

                if (code != MsgPacket.Code)
                {
                    OnError($"Address:{RemoteAddress} Packet Code Error:[{code}]");
                    Close();
                    return;
                }

                var end = buff.Pop_Bool();
                var flag = buff.Pop_Byte();
                var cmd = buff.Pop_Str();
                var msgLen = !end ? buff.Pop_Int() : 0;
                var msg = _msg ?? new MsgPacket(cmd, msgLen > pLen ? msgLen : pLen);

                if (!end && _msg == null)
                {
                    _msg = msg;
                }

                if (msg.Cmd != cmd)
                {
                    OnError($"Address:{RemoteAddress} Packet Cmd Error:[{cmd}]");
                    Close();
                    return;
                }

                msg.Append(buff.Data, buff.ReadPos, packetPos + pLen - buff.ReadPos);
                buff.ReadPos = packetPos + pLen;
                if (end)
                {
                    DispatchMsg(flag, msg);
                    _msg = null;
                }
            }
        }

        private void DispatchMsg(byte flag, MsgPacket msg)
        {
            if (flag != 0)
            {
                OnError($"Address:{RemoteAddress} Cmd:{msg.Cmd} Error:flag err"); 
            }
            else
            {
                if (OnFilterMsg?.Invoke((T)this, msg) == false) return;

                if (CmdHandle.TryGetValue(msg.Cmd, out var handle))
                {
                    try
                    {
                        handle((T)this, msg);
                    }
                    catch (Exception e)
                    {
                        OnError($"Address:{RemoteAddress}  Cmd:{msg.Cmd}  handle cause Error:{e}");
                    }

                    return;
                }

                OnError($"Address:{RemoteAddress} Cmd:{msg.Cmd} Error:No Handle");
            }
            Close();
        }
    }
}