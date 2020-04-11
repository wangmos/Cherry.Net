using Cherry.Net.Extensions;
using System; 

namespace Cherry.Net
{
    public class NetBuff
    {
        private static readonly byte[] EmptyData = new byte[0];

        /// <summary>
        /// 数据
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// 数据长度
        /// </summary>
        public int Length { get; internal set; }

        /// <summary>
        /// 当前读取位置
        /// </summary>
        public int ReadPos;

        /// <summary>
        /// 写入位置
        /// </summary>
        private int _writePos;

        /// <summary>
        /// 当前写入位置
        /// </summary>
        public int WritePos
        {
            get => _writePos;
            set
            {
                if (value > Length) Length = value;
                _writePos = value;
            }
        }

        /// <summary>
        /// 容量
        /// </summary>
        public int Capacity
        {
            get => Data.Length;
            set
            {
                if (value <= Data.Length) return;
                var array = new byte[value];
                if (Length > 0)
                    Array.Copy(Data, array, Length);
                Data = array;
            }
        }


        public NetBuff(int capacity)
        {
            Data = EmptyData;
            Capacity = capacity;
        }

        public NetBuff(byte[] data)
        {
            ResetData(data);
        }

        public NetBuff ResetData(byte[] data)
        {
            Data = data;
            WritePos = Data.Length;
            ReadPos = 0;
            return this;
        }

        /// <summary>
        /// 是否到达数据尾
        /// </summary>
        public bool IsEnd => ReadPos >= Length;


        /// <summary>
        /// 并不真正清空数据 只是重置了相关索引记录
        /// </summary>
        /// <returns></returns>
        public virtual NetBuff Clear()
        {
            ReadPos = WritePos = Length = 0;
            return this;
        }

        /// <summary>
        /// 计算容量
        /// </summary>
        /// <param name="size"></param>
        protected void EnsureCapacity(int size)
        {
            var min = WritePos + size;
            if (Capacity >= min) return;
            //增加一倍
            var num = (Capacity == 0) ? 4 : (Capacity * 2);
            //以4对齐
            if (num < min) num = (min % 4 == 0) ? min : ((min - 1) / 4 + 1) * 4;
            Capacity = num;
        }

        /// <summary>
        /// 长度是否足够 有异常
        /// </summary>
        /// <param name="len"></param>
        private void CheckRead(int len)
        {
            if (ReadPos + len > Length)
                throw new IndexOutOfRangeException("超出有效范围");
        }

        /// <summary>
        /// 反转
        /// </summary>
        /// <returns></returns>
        public NetBuff Reverse(int startIndex,int len)
        {
            Array.Reverse(Data, startIndex, len);
            return this;
        }

        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte this[int index]
        {
            get
            {
                if (index > Length) throw new IndexOutOfRangeException("超出有效范围");
                return Data[index];
            }
        }

        /// <summary>
        /// 取范围
        /// </summary>
        /// <param name="index"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public byte[] this[int index, int len] => GetBytesAt(index, len);

        /// <summary>
        /// 复制
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray() => GetBytesAt(0, Length);

        /// <summary>
        /// 取范围
        /// </summary>
        /// <param name="index"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public byte[] GetBytesAt(int index, int len)
            => index + len > Length
                ? throw new IndexOutOfRangeException("超出有效范围")
                : Data.GetRange(index, len);


        /// <summary>
        /// 当前位置取范围 更新索引
        /// </summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public byte[] GetBytes(int len)
        {
            var bs = GetBytesAt(ReadPos, len);
            ReadPos += len;
            return bs;
        }

        #region Append

        /// <summary>
        /// 追加 不压入长度
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Append(byte[] val) => Append(val, 0, val.Length);

        /// <summary>
        /// 追加 不压入长度
        /// </summary>
        /// <param name="val"></param>
        /// <param name="index"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public NetBuff Append(byte[] val, int index, int len)
        {
            EnsureCapacity(len);
            Array.Copy(val, index, Data, WritePos, len);
            WritePos += len;
            return this;
        }

        /// <summary>
        /// 追加 不压入长度
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Append(NetBuff val) => Append(val.Data, 0, val.Length);


        /// <summary>
        /// 追加 不压入长度
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public NetBuff Append(string str) => Append(str.ToBytes());

        #endregion

        #region Read

        /// <summary>
        /// 读取到
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] ReadTo(byte[] data)
        {
            var i = Data.Find(data, ReadPos);

            //在有效范围内
            if (i >= ReadPos && i + data.Length <= Length)
            {
                var ls = new byte[i - ReadPos];
                Array.Copy(Data, ReadPos, ls, 0, ls.Length);
                ReadPos = i + data.Length; // 移动到查找对象后面
                return ls;
            }

            return null;
        }

        /// <summary>
        /// 读取到
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] ReadTo(string data) => ReadTo(data.ToBytes());

        /// <summary>
        /// 读取到 并返回字符串
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string ReadTo_Str(string data) => ReadTo(data)?.ToEncodingString();

        /// <summary>
        /// 读取一行
        /// </summary> 
        /// <returns></returns>
        public string ReadLine() => ReadTo_Str("\r\n");

        /// <summary>
        /// 移动到
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool MoveTo(byte[] data)
        {
            var i = Data.Find(data, ReadPos);
            if (i >= ReadPos && i + data.Length <= Length)
            {
                ReadPos = i + data.Length; // 移动到查找对象后面 
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移动到
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool MoveTo(string data) => MoveTo(data.ToBytes());

        #endregion

        #region 压入


        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public NetBuff Push(string str) => Push(str.ToBytes());

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(short val)
        {
            EnsureCapacity(2);
            Data[WritePos++] = (byte)(val >> 8);
            Data[WritePos++] = (byte)(val << 8 >> 8);
            return this;
        }

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(ushort val) => Push((short)val);

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(int val)
        {
            EnsureCapacity(4);
            Data[WritePos++] = (byte)(val >> 24);
            Data[WritePos++] = (byte)(val << 8 >> 24);
            Data[WritePos++] = (byte)(val << 16 >> 24);
            Data[WritePos++] = (byte)(val << 24 >> 24);
            return this;
        }

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(uint val) => Push((int)val);

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public unsafe NetBuff Push(float val) => Push(*(int*)(&val));

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public unsafe NetBuff Push(double val) => Push(*(long*)(&val));

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(long val)
        {
            EnsureCapacity(8);
            Data[WritePos++] = (byte)(val >> 56);
            Data[WritePos++] = (byte)(val << 8 >> 56);
            Data[WritePos++] = (byte)(val << 16 >> 56);
            Data[WritePos++] = (byte)(val << 24 >> 56);
            Data[WritePos++] = (byte)(val << 32 >> 56);
            Data[WritePos++] = (byte)(val << 40 >> 56);
            Data[WritePos++] = (byte)(val << 48 >> 56);
            Data[WritePos++] = (byte)(val << 56 >> 56);
            return this;
        }

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(ulong val) => Push((long)val);

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(char val) => Push((short)val);

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(sbyte val) => Push((byte)val);

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(byte val)
        {
            EnsureCapacity(1);
            Data[WritePos++] = val;
            return this;
        }

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(bool val) => Push((byte)(val ? 1 : 0));

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(byte[] val)
        {
            Push(val.Length);
            return Append(val);
        }

        /// <summary>
        /// 压入值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public NetBuff Push(NetBuff val)
        {
            Push(val.Length);
            return Append(val.Data, 0, val.Length);
        }

        #region 常量值

        public NetBuff PushUshort(int val) => Push((short)val);
        public NetBuff PushShort(int val) => Push((short)val);
        public NetBuff PushUlong(int val) => Push((long)val);
        public NetBuff PushLong(int val) => Push((long)val);
        public NetBuff PushByte(int val) => Push((byte)val);
        public NetBuff PushSByte(int val) => Push((sbyte)val);
        public NetBuff PushFloat(int val) => Push((float)val);
        public NetBuff PushDouble(int val) => Push((double)val);
        public NetBuff PushFloat(double val) => Push((float)val);


        #endregion

        #endregion

        #region 弹出 

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public string Pop_Str() => Pop_Bytes().ToEncodingString();

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public short Pop_Short()
        {
            CheckRead(2);
            return (short)(Data[ReadPos++] << 8 | Data[ReadPos++]);
        }

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public ushort Pop_UShort() => (ushort)Pop_Short();

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public int Pop_Int()
        {
            CheckRead(4);
            return Data[ReadPos++] << 24 | Data[ReadPos++] << 16 | Data[ReadPos++] << 8 | Data[ReadPos++];
        }

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public uint Pop_UInt() => (uint)Pop_Int();

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public unsafe float Pop_Float()
        {
            var i = Pop_Int();
            return *(float*)&i;
        }

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public unsafe double Pop_Double()
        {
            var i = Pop_Long();
            return *(double*)&i;
        }

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public long Pop_Long()
        {
            CheckRead(8);
            return (long)Data[ReadPos++] << 56 | (long)Data[ReadPos++] << 48 | (long)Data[ReadPos++] << 40 |
                   (long)Data[ReadPos++] << 32 | (long)Data[ReadPos++] << 24 | (long)Data[ReadPos++] << 16 |
                   (long)Data[ReadPos++] << 8 | Data[ReadPos++];
        }

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public ulong Pop_ULong() => (ulong)Pop_Long();

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public char Pop_Char() => (char)Pop_Short();

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public byte[] Pop_Bytes()
        {
            var len = Pop_Int();
            return GetBytes(len);
        }

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public sbyte Pop_SByte() => (sbyte)Pop_Byte();

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public byte Pop_Byte() => IsEnd
            ? throw new IndexOutOfRangeException("超出有效范围")
            : Data[ReadPos++];

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public bool Pop_Bool() => Pop_Byte() > 0;

        /// <summary>
        /// 弹出值
        /// </summary>
        /// <returns></returns>
        public NetBuff Pop_MsgPacket()
        {
            return new NetBuff(Pop_Bytes());
        }

        #endregion

        #region 弹出 Out

        public NetBuff Pop_Str(out string val)
        {
            val = Pop_Str();
            return this;
        }

        public NetBuff Pop_Short(out short val)
        {
            val = Pop_Short();
            return this;
        }

        public NetBuff Pop_UShort(out ushort val)
        {
            val = Pop_UShort();
            return this;
        }

        public NetBuff Pop_Int(out int val)
        {
            val = Pop_Int();
            return this;
        }

        public NetBuff Pop_UInt(out uint val)
        {
            val = Pop_UInt();
            return this;
        }
        public NetBuff Pop_Float(out float val)
        {
            val = Pop_Float();
            return this;
        }
        public NetBuff Pop_Double(out double val)
        {
            val = Pop_Double();
            return this;
        }

        public NetBuff Pop_Long(out long val)
        {
            val = Pop_Long();
            return this;
        }

        public NetBuff Pop_ULong(out ulong val)
        {
            val = Pop_ULong();
            return this;
        }

        public NetBuff Pop_Char(out char val)
        {
            val = Pop_Char();
            return this;
        }
        public NetBuff Pop_Bytes(out byte[] val)
        {
            val = Pop_Bytes();
            return this;
        }

        public NetBuff Pop_SByte(out sbyte val)
        {
            val = Pop_SByte();
            return this;
        }

        public NetBuff Pop_Byte(out byte val)
        {
            val = Pop_Byte();
            return this;
        }

        public NetBuff Pop_Bool(out bool val)
        {
            val = Pop_Bool();
            return this;
        }

        public NetBuff Pop_MsgPacket(out NetBuff val)
        {
            val = Pop_MsgPacket();
            return this;
        }

        #endregion
         
        /// <summary>
        /// 数据的十六进制字符形式
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Data.ToHexString(" ");
         
    }
}