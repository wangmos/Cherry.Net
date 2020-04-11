using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cherry.Net.Extensions;
using Cherry.Net.Http;
using Cherry.Net.Tcp;
using Cherry.Net.Tcp.Publisher;
using Cherry.Net.Tcp.Subscriber;
using Cherry.Net.Udp;

namespace Cherry.Net
{
    class Program
    {
        static void Main(string[] args)
        {

            TestHttpCHannel();

            Console.ReadKey();
        }

        static void TestHttpCHannel()
        {
            TestHttp.Init();
            TestHttp.Get("/test", (request, response, channel) =>
            {
                Console.WriteLine($"{request.Path} {request.FullPath} {request.ContentText}");
                response.Send("我爱啊你啊啊a");
                return Task.CompletedTask;
            });


            TestHttp.Listener.Start(8878);
        }




        //static async void TestHttp()
        //{
        //    var hl = new HttpListener {AuthenticationSchemes = AuthenticationSchemes.Anonymous};
        //    hl.Prefixes.Add($"http://*:8878/");
        //    hl.Start();

        //    while (true)
        //    {
        //        var hc = await hl.GetContextAsync();

        //        var req = hc.Request;

        //        var res = hc.Response;

        //        HandleRes(res);
        //    } 
        //}

        static async void HandleRes(HttpListenerResponse res)
        {
            res.ContentEncoding = Encoding.UTF8; 
            res.ContentType= "text/plain;charset=UTF-8";
            res.AddHeader("Content-type", "text/plain");

            var data = "我的手机".ToBytes();
            await res.OutputStream.WriteAsync(data, 0, data.Length);
            res.OutputStream.Flush(); 
            res.OutputStream.Close(); 
        }

        static void TestPs()
        {
            var p = new Publisher();
            p.Start(8878);

            Console.WriteLine("启动成功");
            Console.ReadKey();


            var c1 = new Subscriber();
            c1.Connetct("127.0.0.1", 8878);
            var c2 = new Subscriber();
            c2.Connetct("127.0.0.1", 8878);

            c1.OnConnected = () =>
            {
                c1.Subscribe("test1", (subscriber, buff) =>
                {
                    Console.WriteLine($"c1 test1 data:{buff.Pop_Str()}");
                });
                c1.Subscribe("test2", (subscriber, buff) =>
                {
                    Console.WriteLine($"c1 test2 data:{buff.Pop_Str()}");
                });
            };

            c2.OnConnected = () => {
                c2.Subscribe("test2", (subscriber, buff) =>
                {
                    Console.WriteLine($"c2 test2 data:{buff.Pop_Str()}");
                });
                c2.Subscribe("test3", (subscriber, buff) =>
                {
                    Console.WriteLine($"c2 test3 data:{buff.Pop_Str()}");
                });
            };

            Console.WriteLine("连接成功");
            Console.ReadKey();

            p.Publish("test1", new NetBuff(0).Push("publish test1 msg"));
            p.Publish("test2", new NetBuff(0).Push("publish test2 msg"));
            p.Publish("test3", new NetBuff(0).Push("publish test3 msg"));

            Console.WriteLine("发布成功");
            Console.ReadKey();

            c1.Publish("test3", new NetBuff(0).Push("c1 publish test3 msg"));
            c2.Publish("test1", new NetBuff(0).Push("c2 publish test1 msg"));
        }

        static void TestUdp()
        {
            var udp = new TestUdp();
            if (udp.Start(null, 8878))
            {
                Console.WriteLine("启动成功：8878");
            }

            Console.ReadKey();

            var cli = new TestUdp();
            cli.Start(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8878));
            while (true)
            {
                cli.Send("dwdwad低洼地挖多哇多".ToBytes());
            }

            Console.ReadKey();

            udp.Close();
        }

        static void TestTcp()
        {
            int d = 0;
            double t = 0;
            DateTime dt = DateTime.Now;
            TestServer.Init(new NetConfig() { MaxPoolNum = 1 });
            TestServer.Listener.OnSucc = (starter, port) => Console.WriteLine($"启动成功：{port}");
            TestServer.Listener.OnStop = () => Console.WriteLine($"服务器关闭");
            TestServer.OnLoseChannel = server => Console.WriteLine($"{server.Id} lose");
            TestServer.SetCmdHandle("test1", (channel, msg) =>
            {
                var str = msg.Pop_Str();
                Interlocked.Increment(ref d);

                //Console.WriteLine($"{d}");

                if (d == 1) dt = DateTime.Now;
                t += msg.Length;
                if (d == 500000)
                {
                    var sec = DateTime.Now - dt;
                    Console.WriteLine($"sec:{sec.TotalMilliseconds}ms  speed:{t / sec.TotalSeconds}");
                }

                //channel.SendMsg(msg);
            });
            TestServer.Listener.Start(8878);

            Console.ReadKey();

            TestClient.Init(new NetConfig() { MaxPoolNum = 1 });
            TestClient.Connector.OnFail = (connector, ip, port, err) => Console.WriteLine($"连接失败:" + err);
            TestClient.OnGetChannel = client =>
            { 
                for (int j = 0; j < 500000; j++)
                { 
                    var msg = new MsgPacket("test1");
                    msg.Push(3).Push(new string('w',8000)); 

                    //var data = msg.ToArray();

                    //var data = new byte[]
                    //{
                    //    00, 0x27, 0x1A, 0x85, 01, 00, 00, 00, 00, 05, 0x74, 0x65, 0x73, 0x74, 0x31, 00, 00, 00, 0x03, 00, 00, 0x00, 0x10, 0x77,
                    //    0x64, 0x6A, 0x77, 0xE4, 0xBD, 0x8E, 0xE6, 0xB4, 0xBC, 0xE5, 0x9C, 0xB0, 0xE6, 0x8C, 0x96
                    //};

                    client.SendMsg(msg);
                    //client.Send(data, 0, data.Length); 

                    //Thread.Sleep(10);
                    //client.Send("dwdwad低洼地挖多哇多".ToBytes());
                }
            };

            for (int i = 0; i < 1; i++)
            {
                TestClient.Connector.Connect("127.0.0.1", 8878);
                //Thread.Sleep(10);
            }


            //Console.ReadKey();

            //TestServer.Listener.Stop();
            //TestServer.Stop();

            Console.ReadKey();

            GC.Collect();
        }
    }

    class TestHttp : HttpChannel<TestHttp>
    {

    }

    class TestUdp : UdpChannel
    {
        private int i = 0;
        protected override async void OnReceiveData(EndPoint remoteAddr, NetBuff buff)
        {
            var data = buff.ToArray();

            Console.WriteLine($"{Interlocked.Increment(ref i)} {remoteAddr} data:{data.ToEncodingString()}");

            buff.ReadPos = buff.Length;
            //await Send(remoteAddr, data);
        } 
    }

    class TestServer : TcpMsgChannel<TestServer>
    {

        //protected override void OnReceiveData(NetBuff buff)
        //{
        //    var data = buff.ToArray();
        //    Console.WriteLine($"{Interlocked.Increment(ref i)} data:{data.ToEncodingString()}");
        //    buff.ReadPos = buff.Length;

        //    //Send(data, 0, data.Length);
        //}
    }

    class TestClient : TcpMsgChannel<TestClient>
    {
        //static TestClient()
        //{  
        //    SetCmdHandle("test1", (channel, msg) =>
        //    {
        //        //Console.WriteLine($"from server int:{msg.Pop_Int()} str:{msg.Pop_Str()}");
        //        channel.SendMsg(msg);
        //    });
        //}

        //protected override void OnReceiveData(NetBuff buff)
        //{
        //    var data = buff.ToArray();
        //    Console.WriteLine(data.ToEncodingString());
        //    buff.ReadPos = buff.Length; 
        //}
    }
}
