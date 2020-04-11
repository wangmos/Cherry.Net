using System;
using Cherry.Net.Extensions;

namespace Cherry.Net
{
    public class MsgPacket:NetBuff
    {
        /// <summary>
        /// 验证码
        /// </summary>
        public static ushort Code = 6789;

        /// <summary>
        /// 包头字节数 2长度+2验证码+1是否完成+1标识 不含cmd
        /// </summary>
        internal const byte FreeNum = 6;

        /// <summary>
        /// 指令
        /// </summary>
        public string Cmd { get; internal set; }
         
        /// <summary>
        /// 内容开始位置 cmd之后
        /// </summary>
        internal int ContentPos;

        internal MsgPacket(int capacity = 0) : base(capacity > FreeNum ? capacity : FreeNum)
        {
            ReadPos = WritePos = FreeNum;
        }

        public MsgPacket(string cmd, int capacity = 0) : this(capacity)
        {
            Cmd = cmd;
            Push(Cmd);
            ReadPos = ContentPos = WritePos;
        }

        /// <summary>
        /// 自动解出cmd
        /// </summary>
        /// <param name="data"></param> 
        internal MsgPacket(byte[] data) : base(data)
        {
            WritePos = ReadPos = FreeNum;
            Cmd = Pop_Str();
            WritePos = ContentPos = ReadPos;
        }

        /// <summary>
        /// 不改变cmd
        /// </summary>
        /// <returns></returns>
        public override NetBuff Clear()
        {
            return Clear(Cmd);
        }


        /// <summary>
        /// 用新指令清空内容 
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public MsgPacket Clear(string cmd)
        {
            base.Clear();
            ReadPos = WritePos = FreeNum;
            Cmd = cmd;
            Push(Cmd);
            ReadPos = ContentPos = WritePos;
            return this;
        }

        /// <summary>
        /// 内容 不包含指令
        /// </summary>
        /// <returns></returns>
        public byte[] Content => Data.GetRange(ContentPos, Length - ContentPos);

        /// <summary>
        /// 复制
        /// </summary>
        /// <returns></returns>
        public MsgPacket Clone()
        {
            var msg = new MsgPacket(Cmd);
            msg.Append(Data, ContentPos, Length - ContentPos);
            return msg;
        }

        /// <summary>
        /// 打包
        /// </summary>
        /// <param name="end"></param>
        /// <param name="flag">0 用户指令 非0 内部指令</param>
        /// <returns></returns>
        internal MsgPacket Packet(bool end, byte flag)
        {
            if (string.IsNullOrEmpty(Cmd)) throw new InvalidOperationException("Cmd is invalid!!!");
            WritePos = 0;
            return (MsgPacket)PushUshort(Length).Push(Code).Push(end).Push(flag);
        }
    }
}