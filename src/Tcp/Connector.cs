using Cherry.Net.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Cherry.Net.Tcp
{
    public class Connector
    {
        public delegate void FailDlg(Connector connector, string ip, int port, string err); 

        public FailDlg OnFail;
        internal Action<Socket> OnGetSocket;

        private static readonly HashSet<string> LocalHost = new HashSet<string> { "127.0.0.1", "::1", "localhost" };

        public async void Connect(string ip, int port, int reConnectTimeMs = 0, int connectTimeOut = 3000)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            var  remoteEndPoint = new IPEndPoint(LocalHost.Contains(ip.ToLower())
                ? IPAddress.Loopback
                : IPAddress.Parse(ip), port);

            if (connectTimeOut > 0)
            {
                TimerEx.Delay(() =>
                {
                    try
                    {
                        if (socket?.Connected == false)
                        {
                            socket?.Close();
                        }
                    }
                    catch 
                    { 

                    }
                }, connectTimeOut);
            }

            try
            { 
                await socket.ConnectAsync(remoteEndPoint);

                if (socket.Connected)
                {
                    OnGetSocket(socket);
                    return;
                } 
            }
            catch (Exception e)
            {
                socket.Close();
                socket = null;
                OnFail(this, ip, port, e.ToString());
            }

            if (reConnectTimeMs > 0)
            {
                TimerEx.Delay(() => { Connect(ip, port, reConnectTimeMs, connectTimeOut); }, reConnectTimeMs);
            }
        } 
    }
}