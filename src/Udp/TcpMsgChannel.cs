using System;
using System.Collections.Concurrent;
using System.Net;

namespace Cherry.Net.Udp
{
    public abstract class UdpMsgChannel: UdpChannel
    {
        public delegate void MsgHandle(EndPoint addr, MsgPacket msg);

        public delegate bool FilterHandle(EndPoint addr, MsgPacket msg);

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
        /// 注意：在通道关闭时解绑
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

        public void SendMsg(EndPoint toAddr, MsgPacket msg)
        {
            var bufferSize = Config.BufferSize;

            lock (CmdHandle)
            {
                if (msg.Length <= bufferSize)
                {
                    msg.Packet(true, 0);
                    OnSendMsg(msg.Data, 2, msg.Length - 2);
                    Send(toAddr, msg.Data, 0, msg.Length);
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

        protected override void OnReceiveData(EndPoint remoteAddr, NetBuff buff)
        {
            while (buff.ReadPos + 2 <= buff.Length)
            {
                var pLen = buff.Pop_UShort();

                if (pLen < MsgPacket.FreeNum || pLen > buff.Capacity)
                {
                    OnError($"Address:{remoteAddr} Packet Len Error:[{pLen}]");
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
                    OnError($"Address:{remoteAddr} Packet Code Error:[{code}]");
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
                    OnError($"Address:{remoteAddr} Packet Cmd Error:[{cmd}]");
                    Close();
                    return;
                }

                msg.Append(buff.Data, buff.ReadPos, packetPos + pLen - buff.ReadPos);
                buff.ReadPos = packetPos + pLen;
                if (end)
                {
                    DispatchMsg(flag, remoteAddr, msg);
                    _msg = null;
                }
            }
        }

        private void DispatchMsg(byte flag, EndPoint remoteAddr, MsgPacket msg)
        {
            if (flag != 0)
            {
                OnError($"Address:{remoteAddr} Cmd:{msg.Cmd} Error:flag err"); 
            }
            else
            {
                if (OnFilterMsg?.Invoke(remoteAddr, msg) == false) return;

                if (CmdHandle.TryGetValue(msg.Cmd, out var handle))
                {
                    try
                    {
                        handle(remoteAddr, msg);
                    }
                    catch (Exception e)
                    {
                        OnError($"Address:{remoteAddr}  Cmd:{msg.Cmd}  handle cause Error:{e}");
                    }

                    return;
                }

                OnError($"Address:{remoteAddr} Cmd:{msg.Cmd} Error:No Handle");
            }
            Close();
        }
    }
}