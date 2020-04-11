using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cherry.Net.Extensions;
using Cherry.Net.Tcp;
using Cherry.Net.Utils; 

namespace Cherry.Net.Http
{
    public abstract class HttpChannel<T> : TcpChannel<T> where T : HttpChannel<T>,new()
    {
        public delegate Task Handle(HttpRequest request, HttpResponse response, T channel);

        public delegate Task<bool> Filter(HttpRequest request, HttpResponse response, T channel);

        #region Static

        private static readonly ConcurrentDictionary<string, Handle> GetHandle
            = new ConcurrentDictionary<string, Handle>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, Handle> PostHandle
            = new ConcurrentDictionary<string, Handle>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, Handle> AllHandle
            = new ConcurrentDictionary<string, Handle>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSetEx<string> FilePaths = new HashSetEx<string>();

        /// <summary>
        /// 需要过滤的文件请求
        /// </summary>
        public static readonly HashSetEx<string> FillterFilePath = new HashSetEx<string>();
        /// <summary>
        /// 不需要过滤的请求
        /// </summary>
        public static readonly HashSetEx<string> NoFillterPath = new HashSetEx<string>();

        private static readonly Dictionary<int, string> HttpState
            = new Dictionary<int, string>()
            {
                {200, "OK"},
                {302, "Found"},
                {304, "Not Modify"},
                {400, "Bad Request"},
                {403, "Forbidden"},
                {404, "File Not Found"},
                {500, "Internal Server Error"},
            };

        public static Filter OnFilter, OnFilterFile;
        public static Handle OnUnHandle;

        /// <summary>
        /// 主页文件 /index.html
        /// </summary>
        public static string HomePage = "/index.html";

        /// <summary>
        /// 根目录 . or null, or 文件夹,不能为/
        /// </summary>
        public static string RootDir = ".";

        /// <summary>
        /// /login
        /// </summary>
        /// <param name="path"></param>
        /// <param name="handle"></param>
        /// <param name="fillter"></param>
        public static void Get(string path, Handle handle, bool fillter = true)
        {
            if (!fillter) NoFillterPath.Add(path);
            GetHandle[path.ToLower()] = handle;
        }

        /// <summary>
        /// /login
        /// </summary>
        /// <param name="path"></param>
        /// <param name="handle"></param>
        /// <param name="fillter"></param>
        public static void Post(string path, Handle handle, bool fillter = true)
        {
            if (!fillter) NoFillterPath.Add(path);
            PostHandle[path.ToLower()] = handle;
        }

        /// <summary>
        /// /login
        /// </summary>
        /// <param name="path"></param>
        /// <param name="handle"></param>
        /// <param name="fillter"></param>
        public static void All(string path, Handle handle, bool fillter = true)
        {
            if (!fillter) NoFillterPath.Add(path);
            AllHandle[path.ToLower()] = handle;
        }

        /// <summary>
        /// . or / 所有目录和文件
        /// </summary>
        /// <param name="path"></param>
        public static void SetFiles(params string[] path)
        {
            SetPaths(FilePaths, path);
        }

        /// <summary>
        /// . or / 所有目录和文件
        /// </summary>
        /// <param name="path"></param>
        public static void SetFiles(IList<string> path)
        {
            SetPaths(FilePaths, path);
        }

        private static void SetPaths(HashSetEx<string> aches, IList<string> path)
        {
            if (path?.Count > 0)
            {
                for (var i = 0; i < path.Count; i++)
                {
                    var dir = path[i];
                    if (dir == ".") dir = "/";
                    if (dir[0] != '/') dir = '/' + dir;
                    path[i] = dir.ToLower();
                }

                aches.Add(path);
            }
        }

        private static bool GetPath(string path, HashSetEx<string> paths)
        {
            lock (paths)
            {
                return paths.Any(path.StartsWith);
            }
        }

        #endregion

        private HttpRequest _request;

        protected override void OnInitialized()
        {
            _request = null;
        }

        protected override void OnClosed()
        {
            _request?.Clear();
            _request = null;
        }


        protected override void OnReceiveData(NetBuff buff)
        {
            _request = _request ?? new HttpRequest(0) { Address = RemoteAddress };

            if (_request.Analysis(buff, out var err))
            {
                DispatchHandle(_request);
                _request = null;
            }
            else if (err != null)
            {
                buff.ReadPos = buff.Length;
                SendMsg(new HttpResponse(null)
                {
                    StateCode = 500,
                    Close = true
                });
                _request = null;
            }
            else buff.ReadPos = buff.Length;
        }

        private async void DispatchHandle(HttpRequest request)
        {
            Debug.WriteLine($"{Id} {request.Method} {request.FullPath} {DateTime.Now:G}");

            var response = new HttpResponse(request);

            try
            {
                if (request.Path == "/") request.Path = HomePage;

                if (request.Path.Contains(".") && GetPath(request.Path, FilePaths))
                {
                    if (OnFilterFile != null
                        && (FillterFilePath.Contains(request.Path)
                         || FillterFilePath.Contains(Path.GetExtension(request.Path)))
                        && await OnFilterFile(request, response, (T)this) == false)
                    {
                        response.IsFile = true;
                        SendMsg(response);
                        return;
                    }
                    SendFile(request, response);
                    return;
                }

                Handle handAction;
                switch (request.Method)
                {
                    case "GET" when GetHandle.TryGetValue(request.Path, out handAction):
                        break;
                    case "POST" when PostHandle.TryGetValue(request.Path, out handAction):
                        break;
                    default:
                        AllHandle.TryGetValue(request.Path, out handAction);
                        break;
                }

                if (handAction == null)
                {
                    if (OnUnHandle != null)
                        await OnUnHandle(request, response, (T)this);
                    else response.StateCode = 400;
                }
                else
                {
                    if (OnFilter == null
                        || NoFillterPath.Contains(request.Path)
                        || await OnFilter(request, response, (T)this))
                    {
                        await handAction(request, response, (T)this);
                    }
                }
            }
            catch (Exception e)
            {
                OnError($"{RemoteAddress} {e}\r\nPath:{request.Path}\r\nContentText:{request.ContentText}");
                response.StateCode = 500;
            }

            SendMsg(response);
        }

        public void SendFile(HttpRequest request, HttpResponse response, int bufferSize = 4096)
        {
            response.IsFile = false;

            var fn = $"{RootDir}{request.Path}";

            if (!File.Exists(fn))
            {
                //response.StateCode = 404;
                response.Send("你访问的页面不存在");
                SendMsg(response);
                return;
            }

            var modifyDt = new FileInfo(fn).LastWriteTimeUtc;
            var lastModify = request.GetHeader("If-Modified-Since");

            if (!string.IsNullOrEmpty(lastModify))
            {
                var dt = DateTime.Parse("1970-1-01").AddMilliseconds(lastModify.ToLong());
                if (dt >= modifyDt)
                {
                    //无需更新 
                    response.StateCode = 304;
                    SendMsg(response);
                    return;
                }
            }

            try
            {
                var ext = Path.GetExtension(fn);

                switch (ext)
                {
                    case ".html":
                        response.ContentType = HttpContentType.Html;
                        break;
                    case ".js":
                        response.ContentType = HttpContentType.Js;
                        break;
                    case ".css":
                        response.ContentType = HttpContentType.Css;
                        break;
                    case ".ico":
                        response.ContentType = HttpContentType.Ico;
                        break;
                    case ".jpg":
                        response.ContentType = HttpContentType.Jpg;
                        break;
                    case ".png":
                        response.ContentType = HttpContentType.Png;
                        break;
                    case ".gif":
                        response.ContentType = HttpContentType.Gif;
                        break;
                    case ".ttf":
                    case ".eot":
                    case ".svg":
                    case ".woff":
                    case ".woff2":
                        response.ContentType = HttpContentType.Html;
                        break;
                    default:
                        response.ContentType = HttpContentType.Binary;
                        response.Header.Add(("Content-Disposition", $"attachment; filename={response.DownFileName ?? Path.GetFileName(fn)}"));
                        break;
                }

                if (response.ContentType != HttpContentType.Binary)
                {
                    response.Header.Add(("last-modified", (modifyDt.ToTimestampLong() + 1).ToString()));
                    response.Header.Add(("Cache-Control", "public,max-age=31536000,no-cache"));
                }
                using (var fs = new FileStream(fn, FileMode.Open, FileAccess.Read))
                {
                    SendHeader(response, (int)fs.Length);

                    while (true)
                    {
                        var rBs = new byte[bufferSize];
                        var rNum = fs.Read(rBs, 0, bufferSize);
                        if (rNum <= 0) break;
                        Send(rBs, 0, rNum);
                    }
                }
            }
            catch (Exception e)
            {
                OnError($"{RemoteAddress} {e}\r\nPath:{request.Path}\r\nContentText:{request.ContentText}");
                response.StateCode = 500;
                SendMsg(response);
            }
        }

        /// <summary>
        /// 
        /// </summary> 
        /// <param name="msg"></param>
        internal void SendMsg(HttpResponse msg)
        {
            if (msg.StateCode == 200 && msg.IsFile)
            {
                SendFile(msg.Request, msg);
                return;
            }
            var data = msg.Buffer.Data;
            var len = msg.Buffer.Length;

            if (msg.UseGzip && len > 0)
            {
                data = GZip.Compress(data, 0, len);
                len = data.Length;
                msg.Header.Add(("Content-Encoding", "gzip"));
            }

            SendHeader(msg, len);

            if (len > 0) Send(data, 0, len);
        }

        private void SendHeader(HttpResponse msg, int len)
        {
            if (msg.Close)
            {
                msg.Header.Add(("connection", "close"));
            }

            var bs = new NetBuff(256);
            var contentStr = new StringBuilder($"HTTP/1.1 {msg.StateCode} {HttpState[msg.StateCode]}\r\n");
            if (msg.StateCode == 200)
                contentStr.Append($@"Content-type: {msg.ContentType}
Content-Length: {len}
");

            bs.Append(Encoding.UTF8.GetBytes(contentStr.ToString()));

            if (msg.Header != null)
            {
                foreach (var kv in msg.Header)
                {
                    bs.Append($"{kv.Item1}: {kv.Item2}\r\n");
                }
            }

            if (msg.Cookies != null)
            {
                foreach (var kv in msg.Cookies)
                {
                    bs.Append($"Set-Cookie: {kv.Item1}={kv.Item2}; Path=/; HttpOnly\r\n");
                }
            }

            bs.Append("\r\n");

            Send(bs.Data, 0, bs.Length);
        }
    }
} 
