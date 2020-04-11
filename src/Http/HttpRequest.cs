using System;
using System.Collections.Generic;
using Cherry.Net.Extensions;
using Cherry.Net.Utils;

namespace Cherry.Net.Http
{
    public class HttpRequest
    {
        /// <summary>
        /// 内容最大长度
        /// </summary>
        public static int MaxContextLength = 0;

        /// <summary>
        /// 完整路径 包含参数
        /// </summary>
        public string FullPath { get; internal set; }

        /// <summary>
        /// 请求的路径 不包括参数
        /// </summary>
        public string Path { get; internal set; }

        /// <summary>
        /// GET or POST
        /// </summary>
        public string Method { get; internal set; }

        /// <summary>
        /// Get的参数
        /// </summary>
        public string QueryStr { get; internal set; }

        /// <summary>
        /// Get的参数
        /// </summary>
        public readonly Dictionary<string, string> QueryParams =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Post的参数
        /// </summary>
        public readonly Dictionary<string, string> PostParams =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Post的参数
        /// </summary>
        public readonly Dictionary<string, byte[]> Files =
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 请求头
        /// </summary>
        public readonly Dictionary<string, string> Header =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public readonly Dictionary<string, string> Cookies =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 取参数
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public string this[string arg]
            => (QueryParams.TryGetValue(arg, out var val)
                   ? val
                   : (PostParams.TryGetValue(arg, out val) ? val : "")) ?? "";

        /// <summary>
        /// 内容文本
        /// </summary>
        public string ContentText { get; internal set; } = "";

        /// <summary>
        /// 内容类型
        /// </summary>
        public string ContentType { get; internal set; } = "";

        /// <summary>
        /// json对象 
        /// </summary>
        public dynamic Json;


        /// <summary>
        /// 客户端地址
        /// </summary>
        public string Address { get; internal set; } = "";

        private readonly NetBuff _buffer;
        private bool _headerOk;
        internal string FormKeyStr = "";
        public byte[] ContentBytes;
        public int ContentLength { get; internal set; }
        internal bool UseGzip;

        internal HttpRequest(int bufferSize)
        {
            _buffer = new NetBuff(bufferSize);
        }

        public int Count => _buffer.Length;

        internal void Clear()
        {
            _headerOk = false;
            FormKeyStr = "";
            ContentLength = 0;

            ContentText = "";

            Header.Clear();
            PostParams.Clear();
            QueryParams.Clear();
            Cookies.Clear();
            Files.Clear();
            QueryStr = "";

            _buffer.Clear();
            UseGzip = false;
            Address = "";
        }

        public string GetCookie(string name)
        {
            return Cookies.TryGetValue(name, out var val) ? val : "";
        }

        public string GetHeader(string name)
        {
            return Header.TryGetValue(name, out var val) ? val : "";
        }

        public byte[] GetFile(string name)
        {
            return Files.TryGetValue(name, out var val) ? val : null;
        }

        /// <summary>
        /// -1 错误 0 正常 1 完成 2
        /// </summary>
        /// <param name="buffer"></param> 
        /// <param name="err"></param>
        /// <returns></returns>
        internal bool Analysis(NetBuff buffer, out string err)
        {
            err = null;
            return !_headerOk ? AnalysisHeader(buffer, out err) : AnalysisContent(buffer);
        }

        internal bool AnalysisHeader(NetBuff buffer, out string err)
        {
            err = null;
            _buffer.Append(buffer);

            if (_buffer.Length > 4)
            {
                var method = _buffer.Data.ToEncodingString(0, 4).ToUpper();

                if (method != "GET " && method != "POST")
                {
                    err = "非法数据请求";
                    return false;
                }
            }

            var headerBs = _buffer.ReadTo("\r\n\r\n");
            if (headerBs == null) return false;

            var headerStr = headerBs.ToEncodingString();

            var headers = headerStr.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

            if (headers.Length > 0)
            {
                var line = headers[0];
                Method = line.GetSubStr("", " ").ToUpper();
                FullPath = line.GetSubStr(" ").RemoveLast(9); // HTTP/1.1

                if (FullPath.Contains("?"))
                {
                    Path = FullPath.GetSubStr("", "?").UrlDecode().ToLower();
                    QueryStr = FullPath.GetSubStr("?");
                }
                else Path = FullPath.UrlDecode().ToLower();

                //解析get参数
                if (QueryStr != null)
                {
                    foreach (var str in QueryStr.Split('&'))
                    {
                        if (str.Contains("="))
                            QueryParams[str.GetSubStr("", "=")] = str.GetSubStr("=");
                    }
                }

                for (var i = 1; i < headers.Length; i++)
                {
                    line = headers[i];
                    var headName = line.GetSubStr("", ":").Trim();
                    var headValue = line.GetSubStr(":").Trim();
                    Header[headName] = headValue;

                    if (headName.Equals("content-length", StringComparison.OrdinalIgnoreCase))
                    {
                        ContentLength = headValue.ToInt();

                        if (MaxContextLength > 0 && ContentLength > MaxContextLength)
                        {
                            err = "超过最大请求长度";
                            return false;
                        }
                    }
                    else if (headName.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                    {
                        ContentType = headValue.ToLower();

                        if (headValue.ToLower().Contains(HttpContentType.FormData))
                        {
                            FormKeyStr = $"--{headValue.GetSubStr("boundary=")}";
                        }
                    }
                    else if (headName.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        UseGzip = headValue.ToLower().Contains("gzip");
                    }
                    else if (headName.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var kv in headValue.Split(";"))
                        {
                            var cookie = kv.Split("=");
                            Cookies[cookie[0].Trim()] = cookie[1];
                        }
                    }
                }

                _headerOk = true;

                if (ContentLength > 0)
                {
                    if (ContentLength + _buffer.ReadPos <= _buffer.Length)
                    {
                        buffer.ReadPos = buffer.Length - (_buffer.Length - _buffer.ReadPos - ContentLength);
                        AnalysisContent(null);
                        return true;
                    }

                    _buffer.Capacity = ContentLength + _buffer.ReadPos;
                }
                else
                {
                    buffer.ReadPos = buffer.Length - (_buffer.Length - _buffer.ReadPos);
                    return true;
                }
            }
            else
            {
                err = "解析错误";
            }
            return false;
        }

        internal bool AnalysisContent(NetBuff buffer)
        {
            if (buffer?.Length > 0)
            {
                _buffer.Append(buffer);
            }

            if (ContentLength + _buffer.ReadPos > _buffer.Length) return false;

            if (buffer != null)
                buffer.ReadPos = buffer.Length - (_buffer.Length - ContentLength - _buffer.ReadPos);

            ContentBytes = UseGzip
                ? GZip.Decompress(_buffer.Data, _buffer.ReadPos, ContentLength)
                : _buffer.GetBytesAt(_buffer.ReadPos, ContentLength);

            //post 表单
            if (FormKeyStr != "")
            {
                var readBytes = new NetBuff(ContentBytes);
                //开始解析Form   
                readBytes.ReadPos += FormKeyStr.Length + 2;

                var strRead = readBytes.ReadLine();
                while (!string.IsNullOrEmpty(strRead))
                {
                    var pName = strRead.GetSubStr("\"", "\"");
                    var fName = strRead.GetSubStr("filename=\"", "\"");

                    readBytes.ReadPos -= 2;
                    readBytes.MoveTo("\r\n\r\n"); //值开始位置

                    if (string.IsNullOrEmpty(fName))
                    {
                        PostParams[pName] = readBytes.ReadTo_Str($"\r\n{FormKeyStr}");
                    }
                    else
                    {
                        Files[pName?.Length > 0 ? pName : System.IO.Path.GetFileName(fName)] = readBytes.ReadTo($"\r\n{FormKeyStr}");
                    }

                    readBytes.ReadPos += 2;
                    strRead = readBytes.ReadLine();

                    if (strRead == "" || strRead == "--")
                    {
                        break;
                    }
                }
            }
            else if (ContentType.Contains(HttpContentType.Form)
                     || ContentType.Contains(HttpContentType.Json)
                     || ContentType.Contains(HttpContentType.Text)
                     || ContentType.Contains(HttpContentType.Html)
                     || ContentType.Contains(HttpContentType.XHtml)
                     || ContentType.Contains(HttpContentType.Xml))
            {
                ContentText = ContentBytes.ToEncodingString();

                if (ContentType.Contains(HttpContentType.Form))
                {
                    foreach (var str in ContentText.Split('&'))
                    {
                        if (str.Contains("="))
                            PostParams[str.GetSubStr("", "=")] = str.GetSubStr("=");
                    }
                }
                else if (ContentType.Contains(HttpContentType.Json))
                {
                    Json.Decode(ContentText, out Json);
                }
            }

            return true;
        }
    }
} 
