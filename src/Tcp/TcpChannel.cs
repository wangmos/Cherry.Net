using Cherry.Net.Utils;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

namespace Cherry.Net.Tcp
{
    public abstract class TcpChannel<T> where T:TcpChannel<T>,new()
    {
        #region static
        
        public static ConcurrentDictionary<uint, T> Channels;
        protected static UniquePool<T> Pool;
        protected static NetConfig Config;

        public static Action<T> OnGetChannel, OnLoseChannel;

        public static Listener Listener;
        public static Connector Connector;
        private static int _curId;

        public static void Init(NetConfig config = null)
        {
            Config = config ?? new NetConfig();
            Channels = new ConcurrentDictionary<uint, T>(Environment.ProcessorCount, Config.MaxPoolNum);
            Pool = new ConcurrentUniquePool<T>(Config.MaxPoolNum);

            Listener = new Listener {OnGetSocket = OnGetSocket};
            Connector = new Connector { OnGetSocket = OnGetSocket };
        }

        public static T GetChannel(uint id)
        {
            return Channels.TryGetValue(id, out var channel) ? channel : null;
        }

        public static void Stop()
        {
            foreach (var channel in Channels.Values)
            {
                channel.Close();
            }
        }

        public static void Close(uint id)
        {
            if (Channels.TryGetValue(id, out var channel))
            {
                channel.Close();
            }
        }

        private static void OnGetSocket(Socket socket)
        { 
            var curId = (uint)Interlocked.Increment(ref _curId);
            while (curId == 0 || Channels.ContainsKey(curId))
            {
                curId = (uint)Interlocked.Increment(ref _curId);
            }

            var channel = Pool.Pop();
            channel.Id = curId;
            Channels[curId] = channel;

            channel.Init(socket);
            OnGetChannel?.Invoke(channel);
        }

        private static byte[] KeepAlive(bool enable, int firstTime, int perTime)
        {
            var inOptionValues = new byte[12];//3个int
            BitConverter.GetBytes(enable ? 1 : 0).CopyTo(inOptionValues, 0); //是否启用
            BitConverter.GetBytes(firstTime).CopyTo(inOptionValues, 4);//第一次检测的毫秒
            BitConverter.GetBytes(perTime).CopyTo(inOptionValues, 8);//检测间隔
            return inOptionValues;
        }

        #endregion

        public uint Id { get; protected set; }

        private Socket _socket;
        private bool _isSend, _isReceive; 
         
        /// <summary>
        /// 处理完成后 使用 buffer.ReadPos 标记处理到的位置
        /// </summary>
        protected virtual void OnReceiveData(NetBuff buff) { }
        /// <summary>
        /// 初始化完成
        /// </summary>
        protected virtual void OnInitialized() { }
        /// <summary>
        /// 已经关闭
        /// </summary>
        protected virtual void OnClosed() { }
        /// <summary>
        /// 发生异常
        /// </summary>
        /// <param name="err"></param>
        protected virtual void OnError(string err) { }
         
        private readonly NetBuff buffer = new NetBuff(0);
         
        /// <summary>
        /// 是否关闭
        /// </summary>
        public bool Closed { get; private set; }
        /// <summary>
        /// 上次交互时间
        /// </summary>
        public DateTime InteractiveTime { get; private set; }
        /// <summary>
        /// 连接时间
        /// </summary>
        public DateTime ConnectTime { get; private set; } 
        /// <summary>
        /// 远程地址
        /// </summary>
        public string RemoteAddress { get; private set; }
         
        protected TcpChannel()
        {
            buffer.Capacity = Config.BufferSize;  
        }

        /// <summary>
        /// 初始化
        /// </summary>
        internal void Init(Socket socket)
        {
            Closed = false;
            buffer.Clear(); 
             
            ConnectTime = DateTime.Now;
            InteractiveTime = DateTime.Now;

            _socket = socket;
             
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true); 
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _socket.IOControl(IOControlCode.KeepAliveValues, KeepAlive(true, 30 * 1000, 3 * 1000), null);

            //netsh winsock reset
            try
            {
                RemoteAddress = socket.RemoteEndPoint?.ToString();
            }
            catch
            {
                //OnError(e.ToString());
            } 

            OnInitialized();

            Receive();

        }

        private async void Receive()
        {
            try
            {
                while (true)
                {
                    if (_socket != null)
                    {
                        if (buffer.ReadPos > 0)
                        {
                            buffer.Length = buffer.Length - buffer.ReadPos;
                            Array.Copy(buffer.Data, buffer.ReadPos,
                                buffer.Data, 0, buffer.Length);
                        }

                        _isReceive = true;
                        var len = await _socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer.Data, buffer.Length, buffer.Capacity - buffer.Length),
                            SocketFlags.None);

                        _isReceive = false;

                        if (len > 0)
                        {
                            InteractiveTime = DateTime.Now;
                            buffer.WritePos = len + buffer.Length;
                            buffer.ReadPos = 0;

                            OnReceiveData(buffer);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                OnError($"{RemoteAddress} {e}");
            }

            _isReceive = false;
            Close();
        } 

        public async void Send(byte[] data, int index = 0, int len = 0)
        {
            if (len == 0) len = data.Length;
            if (data == null || len == 0) return;
            try
            {
                if (_socket != null)
                { 
                    _isSend = true;
                    var sendLen = await _socket.SendAsync(new ArraySegment<byte>(data, index, len), SocketFlags.None);
                    _isSend = false;

                    if (sendLen == len)
                    {
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                OnError($"{RemoteAddress} {e}");
            }

            _isSend = false;
            Close();
        } 

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            lock (buffer)
            {
                if (_socket != null)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception e)
                    {
                        OnError($"{RemoteAddress} {e}");
                    }
                    finally
                    {
                        _socket.Close();
                        _socket = null;
                    }
                }

                if (!_isReceive && !_isSend && !Closed)
                {
                    Closed = true;
                }
                else return;
            }

            if (Channels.TryRemove(Id, out _))
            {
                Pool.Push((T)this);
                OnLoseChannel?.Invoke((T)this);
            }

            OnClosed(); 
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Connect(string ip, int port)
        {
            var connector = new Connector
            {
                OnGetSocket = Init,
                OnFail = (_1, _2, _3, err) => OnConnectFail(ip, port, err)
            };
            connector.Connect(ip, port);
        }

        public virtual void OnConnectFail(string ip, int port, string err)
        {

        }
    }
}