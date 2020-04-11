using System;
using System.Net;
using System.Net.Sockets;

namespace Cherry.Net.Udp
{
    public class UdpChannel
    {  
        private Socket _socket; 
        private readonly NetBuff buffer;
        public EndPoint _remoteAddr { get; private set; }
        private bool _isSend, _isReceive; 
        protected NetConfig Config;
        public DateTime InteractiveTime { get; protected set; } 

        public UdpChannel(NetConfig config = null)
        {
            Config = config ?? new NetConfig();
            buffer = new NetBuff(Config.BufferSize); 
        } 

        protected virtual void OnSucc(int port)
        {
             
        }
         
        protected virtual void OnFail(int port, string err)
        {

        }

        protected virtual void OnError(string err)
        {

        }

        protected virtual void OnClose()
        {

        }

        /// <summary>
        /// 处理完成后 使用 buffer.ReadPos 标记处理到的位置
        /// </summary>
        protected virtual void OnReceiveData(EndPoint remoteAddr, NetBuff buff)
        {

        }

        public bool Start(EndPoint remoteAddr, int bindPort = 0)
        {
            if (_socket != null)
            {
                OnFail(bindPort, "server already started");
                return false;
            }
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var addr = new IPEndPoint(IPAddress.Any, bindPort);
            _remoteAddr = remoteAddr ?? new IPEndPoint(IPAddress.Any, 0);
            try
            { 
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _socket.Bind(addr);

                OnSucc(bindPort);

                StartReceive();

                return true;
            }
            catch (Exception e)
            {
                OnFail(bindPort, e.ToString());
            }

            return false;
        }

        private async void StartReceive()
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
                        var receInfo = await _socket.ReceiveFromAsync(new ArraySegment<byte>(buffer.Data, buffer.Length, buffer.Capacity - buffer.Length), SocketFlags.None, _remoteAddr);
                        _isReceive = false;
                        if (receInfo.ReceivedBytes > 0)
                        {
                            InteractiveTime = DateTime.Now;
                            buffer.ReadPos = 0;
                            buffer.WritePos = receInfo.ReceivedBytes + buffer.Length;

                            OnReceiveData(receInfo.RemoteEndPoint, buffer);
                        }
                    }
                }
            }
            catch
            {

            }
            _isReceive = false;
            Close();
        } 

        public void Send(byte[] data, int index = 0, int len = 0)
        {
            Send(_remoteAddr, data, index, len);
        }

        public async void Send(EndPoint remoteAddr, byte[] data, int index = 0, int len = 0)
        {
            if (len == 0) len = data.Length;
            if (data == null || len == 0) return;

            try
            {
                if (_socket != null)
                { 
                    _isSend = true;
                    var sendLen = await _socket.SendToAsync(new ArraySegment<byte>(data, index, len), SocketFlags.None, remoteAddr);
                    _isSend = false;
                    if (len != sendLen)
                    {
                        Close();
                    }
                }
                return;
            }
            catch  
            {

            }
            _isSend = false;
            Close();
        }
         
        public void Close()
        { 
            lock(buffer)
            {
                if (_socket != null)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception e)
                    {
                        OnError($"{e}");
                    }
                    finally
                    {
                        _socket.Close();
                        _socket = null;
                    }
                } 

                if (_isSend || _isReceive)
                {
                    return;
                } 
            }

            OnClose();
        }
         
    }
}