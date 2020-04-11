using System.Collections.Generic;
using Cherry.Net.Utils;

namespace Cherry.Net.Http
{
    /// <summary>
    /// 最后一定要调用End 完成发送
    /// </summary>
    public class HttpResponse
    {
        /// <summary>
        /// 状态码 200=成功(默认) 302=重定向 304=无需更新 400=未知请求 403=权限不足 404=不存在 500=服务器内部错误
        /// </summary>
        public int StateCode = 200;

        /// <summary>
        /// 发送内容类型
        /// </summary>
        public string ContentType = HttpContentType.Text;

        /// <summary>
        /// 是否压缩内容
        /// </summary>
        public bool UseGzip;

        /// <summary>
        /// 附加头
        /// </summary>
        public List<(string, string)> Header
            = new List<(string, string)>();
        /// <summary>
        /// 附加cookies
        /// </summary>
        public List<(string, string)> Cookies
            = new List<(string, string)>();

        /// <summary>
        /// 下载显示名字 null=不修改
        /// </summary>
        public string DownFileName;

        /// <summary>
        /// 是否关闭连接
        /// </summary>
        public bool Close;

        internal HttpRequest Request;

        /// <summary>
        /// 是否请求的文件
        /// </summary>
        internal bool IsFile;

        internal readonly NetBuff Buffer = new NetBuff(256);


        internal HttpResponse(HttpRequest request)
        {
            Request = request;
        }

        public HttpResponse Send(string msg)
        {
            Buffer.Append(msg);
            return this;
        }

        public HttpResponse Send(byte[] msg)
        {
            ContentType = HttpContentType.Binary;
            Buffer.Append(msg);
            return this;
        }

        public HttpResponse Send(object msg)
        {
            ContentType = HttpContentType.Json;
            Buffer.Append(Json.Encode(msg));
            return this;
        }

        public void Redirect(string url)
        {
            StateCode = 302;
            Header.Add(("Location", url));
        }
    }
} 
