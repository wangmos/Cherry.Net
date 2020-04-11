using System;
using System.Net;
using System.Net.Sockets;

namespace Cherry.Net.Tcp
{
    public sealed class Listener 
    {
        public delegate void FailDlg(Listener listener, int port, string err); 
        public delegate void SuccDlg(Listener listener, int port); 
          
        public FailDlg OnFail; 
        public SuccDlg OnSucc;
        public Action OnStop;
        internal Action<Socket> OnGetSocket;

        private Socket _socket;

        public bool Start(int port)
        {
            if (_socket != null)
            {
                OnFail?.Invoke(this,port, "server already started");
                return false;
            } 
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var localEp = new IPEndPoint(IPAddress.Any, port);
            try
            { 
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _socket.Bind(localEp);
                _socket.Listen(int.MaxValue);  

                OnSucc?.Invoke(this, port);

                StartAccept();
                 
                return true;
            }
            catch (Exception e)
            {
                OnFail?.Invoke(this, port, e.ToString());
            }
             
            return false;
        }

        private async void StartAccept()
        {
            try
            {
                while (true)
                {
                    if (_socket != null)
                    {
                        var client = await _socket.AcceptAsync();
                        if(client == null) break;
                        OnGetSocket(client);
                    }
                }
            }
            catch
            {

            }

            Stop();
        }

        public void Stop()
        {
            lock (this)
            { 
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                    OnStop?.Invoke();
                }
            }

        }  
    }
}