using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cherry.Net.Extensions;
using Cherry.Net.Utils;

namespace Cherry.Net.Http
{
    public class BoundaryInfo
    {
        public readonly string Boundary;
        public readonly string BeginBoundary;
        public readonly string EndBoundary;
        public readonly string ContentType;
        public readonly MemoryStream MemStream = new MemoryStream();
        public BoundaryInfo()
        {
            Boundary = DateTime.Now.Ticks.ToString("x");
            BeginBoundary = $"--{Boundary}\r\n";
            EndBoundary = $"--{Boundary}--\r\n";

            ContentType = $"{HttpContentType.FormData}; boundary={Boundary}";
        }

        public void PacketForm(Dictionary<string, object> forms)
        {
            if (forms?.Count > 0)
            {
                foreach (var ky in forms)
                {
                    var bytes = Encoding.UTF8.GetBytes($"{BeginBoundary}Content-Disposition: form-data; name=\"{ky.Key}\";\r\n\r\n{ky.Value}\r\n");
                    MemStream.Write(bytes, 0, bytes.Length);
                }
            }
        }

        public void PacketFormFile(byte[] data, string fileFormName, string uploadFileName = null)
        {
            var bytes = Encoding.UTF8.GetBytes($"{BeginBoundary}Content-Disposition: form-data; name=\"{fileFormName}\";filename=\"{uploadFileName ?? fileFormName}\"\r\nContent-Type:{HttpContentType.Binary}\r\n\r\n");
            MemStream.Write(bytes, 0, bytes.Length);

            MemStream.Write(data, 0, data.Length);
        }

        public void PacketFormEnd()
        {
            var bytes = Encoding.UTF8.GetBytes($"\r\n{EndBoundary}");
            MemStream.Write(bytes, 0, bytes.Length);
        }

        public byte[] ToBytes() => MemStream.ToArray();
    }

    public class HttpHelperAttachInfo
    {
        public string Referer;
        public string ContentType;
        public int ReGetNum;
        public int TimeOut;
        public bool UseGzip;
    }

    public class HttpHelper
    {
        #region fileds
        public static int DefaultConnectionLimit = 65535;

        public bool AllowAutoRedirect = true;

        public Dictionary<string, Dictionary<string, Cookie>> DicCookies
            = new Dictionary<string, Dictionary<string, Cookie>>();

        public string ContentType = HttpContentType.Form;

        public bool UseGzip = false;

        public string Accept = HttpContentType.All;

        public string UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";

        public DecompressionMethods AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        public Encoding Encoding = Encoding.UTF8;

        public List<string> CertificateList = new List<string>();

        public bool NeedFixCookie = false;

        public bool NeedClearCookie = false;

        public WebHeaderCollection ResponseHeader;

        public WebProxy MyProxy = null;

        public int TimeOut;

        public string Referer = "";

        public string CharacterSet = "";

        public delegate void GetBytesCallBack(byte[] data, int len);

        public int ReGetNum = 3;

        #endregion

        static HttpHelper()
        {
            ServicePointManager.Expect100Continue = false; //相当于询问服务器是否愿意接受数据传输 false避免版本问题造成的不相应
            ServicePointManager.DefaultConnectionLimit = DefaultConnectionLimit;//设置并发连接数限制上额 
            ServicePointManager.UseNagleAlgorithm = false; //加快效率 不实用延迟算法
        }

        public HttpHelper(int timeOut = 5000)
        {
            TimeOut = timeOut;
        }


        #region cookies
        public void AddCookie(string name, string val, string domain, string path = "/")
        {
            var key = $"{domain} {path}";
            if (!DicCookies.TryGetValue(key, out var valDic))
            {
                DicCookies[key] = valDic = new Dictionary<string, Cookie>();
            }
            if (!valDic.TryGetValue(name, out var cookieVal) || cookieVal.Value != val)
            {
                valDic[name] = new Cookie(name, val, path, domain);
            }
        }
        public void AddCookie(Dictionary<string, string> cookies, string domain, string path = "/")
        {
            foreach (var kv in cookies)
            {
                AddCookie(kv.Key, kv.Value, domain, path);
            }
        }

        private void GetAllCookies(CookieContainer cookieContainer)
        {
            if (cookieContainer == null) return;
            var cookies = new List<Cookie>();
            var domains = cookieContainer.InvokeValue<Hashtable>("m_domainTable");
            foreach (var val in domains.Values)
            {
                var ls = val.InvokeValue<SortedList>("m_list");
                foreach (CookieCollection lsValue in ls.Values)
                {
                    foreach (Cookie o in lsValue)
                    {
                        var key = $"{o.Domain} {o.Path}";
                        if (!DicCookies.TryGetValue(key, out var valDic))
                        {
                            DicCookies[key] = valDic = new Dictionary<string, Cookie>();
                        }
                        if (!valDic.TryGetValue(o.Name, out var cookieVal) || cookieVal.Value != o.Value)
                        {
                            cookies.Add(o);
                        }
                    }
                }
            }

            foreach (var o in cookies)
            {
                var key = $"{o.Domain} {o.Path}";
                DicCookies[key][o.Name] = o;
            }
        }

        private CookieContainer CollectionCookies()
        {
            var cookieContainer = new CookieContainer();
            foreach (var dicValue in DicCookies.Values)
            {
                foreach (var dicValueValue in dicValue.Values)
                {
                    cookieContainer.Add(dicValueValue);
                }
            }

            return cookieContainer;
        }

        public void PrintCookies()
        {
            foreach (var dictionary in DicCookies.Values)
            {
                foreach (var cookie in dictionary.Values)
                {
                    Debug.WriteLine($"{cookie.Name} {cookie.Value} {cookie.Path} {cookie.Domain}\r\n");
                }
            }
        }
        #endregion


        private HttpWebRequest GetRequest(string url, byte[] postData = null, HttpHelperAttachInfo attachInfo = null)
        {
            HttpWebRequest httpWebRequest = null;
            try
            {
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = "http://" + url;

                httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

                httpWebRequest.ServicePoint.Expect100Continue = false;
                httpWebRequest.ServicePoint.ConnectionLimit = DefaultConnectionLimit;

                if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase)) //如果是https类型的 需要携带证书
                {
                    ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
                    httpWebRequest.ProtocolVersion = HttpVersion.Version11;
                    if (CertificateList != null)
                    {
                        foreach (var cer in CertificateList)
                        {
                            if (File.Exists(cer))
                                httpWebRequest.ClientCertificates.Add(
                                    new System.Security.Cryptography.X509Certificates.X509Certificate(cer));
                        }
                    }
                }
                else httpWebRequest.ProtocolVersion = HttpVersion.Version10;

                httpWebRequest.Method = (postData != null) ? "POST" : "GET";

                if (NeedClearCookie) httpWebRequest.Headers["Cookie"] = "";
                else if (NeedFixCookie) httpWebRequest.CookieContainer = CollectionCookies();//设置了这个后 自定义的Cookie会被替换掉

                //代理
                if (MyProxy != null) httpWebRequest.Proxy = MyProxy;

                httpWebRequest.AllowAutoRedirect = AllowAutoRedirect;
                httpWebRequest.ContentType = attachInfo?.ContentType ?? ContentType;

                httpWebRequest.Accept = Accept;
                httpWebRequest.UserAgent = UserAgent;
                httpWebRequest.KeepAlive = false;

                //设置超时
                var timeOut = attachInfo?.TimeOut > 0 ? attachInfo.TimeOut : TimeOut;
                if (timeOut > 0)
                {
                    httpWebRequest.ReadWriteTimeout = timeOut;
                    httpWebRequest.Timeout = timeOut;
                }

                //这个标签主要是告诉服务器本地的缓存的页面的时间  以便服务器 根据这个时间 和 服务器文件最后修改时间进行比较  决定是否刷新本地的缓存
                //this.IfModifiedSince = DateTime.Now.AddHours(8);
                //if (this.IfModifiedSince != DateTime.ParseJson("00:00:00"))
                //httpWebRequest.IfModifiedSince = this.IfModifiedSince;

                //请求的原地址 
                httpWebRequest.Referer = attachInfo?.Referer ?? Referer ?? url;

                //压缩
                httpWebRequest.AutomaticDecompression = AutomaticDecompression;
                if (httpWebRequest.Method == "POST" && postData?.Length > 0)  //如果是Post递交数据，则写入传的字符串数据
                {
                    if (attachInfo?.UseGzip == true || UseGzip)
                    {
                        postData = GZip.Compress(postData, 0, postData.Length);
                        httpWebRequest.Headers["Content-Encoding"] = "gzip";
                    }

                    httpWebRequest.ContentLength = postData.Length;//提交数据长度
                    var stream = httpWebRequest.GetRequestStream();//请求写入流
                    stream.Write(postData, 0, postData.Length);
                    stream.Close();
                }
                httpWebRequest.UnsafeAuthenticatedConnectionSharing = true;

                return httpWebRequest;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                httpWebRequest?.Abort();
                return null;
            }
        }

        private HttpWebResponse GetResponse(HttpWebRequest httpWebRequest, HttpHelperAttachInfo attachInfo)
        {
            var num = attachInfo?.ReGetNum > 0 ? attachInfo.ReGetNum : (ReGetNum > 0 ? ReGetNum : 1);
            for (int i = 0; i < num; i++)
            {
                if (httpWebRequest == null) return null;
                HttpWebResponse httpWebResponse = null;
                try
                {
                    httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    ResponseHeader = httpWebResponse.Headers;
                    CharacterSet = httpWebResponse.CharacterSet;
                    GetAllCookies(httpWebRequest.CookieContainer);

                    return httpWebResponse;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    httpWebResponse?.Close();
                }
                Thread.Sleep(200 * (i + 1));
            }

            return null;
        }

        private static byte[] GetBytes(WebResponse httpWebResponse)
        {
            var responseStream = httpWebResponse?.GetResponseStream();
            try
            {
                if (responseStream != null)
                {
                    var bs = new byte[4096];
                    using (var ms = new MemoryStream())
                    {
                        int readLen;
                        while ((readLen = responseStream.Read(bs, 0, bs.Length)) > 0)
                        {
                            ms.Write(bs, 0, readLen);
                        }
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                responseStream?.Close();
            }
            return null;
        }

        private static Image GetImage(WebResponse response)
        {
            var responseStream = response?.GetResponseStream();
            try
            {
                if (responseStream != null)
                {
                    var img = Image.FromStream(responseStream, true, true);
                    return img;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                responseStream?.Close();
            }
            return null;
        }

        private string GetHtml(byte[] content)
        {
            if (content == null) return null;

            if (content.Length <= 0) return "";
            var en = CharacterSet?.Length > 0 ? Encoding.GetEncoding(CharacterSet) : Encoding.UTF8;
            var html = en.GetString(content);
            try
            {
                var charset = html.MatchOne("charset=\"?([\\w-]+)")
                              ?? html.MatchOne(@"encoding=.?([\w-]+)");
                if (charset != CharacterSet && charset?.ToLower().Contains(en.HeaderName.ToLower()) == false)
                {
                    en = Encoding.GetEncoding(charset == "iso-8859-1" ? "gbk" : charset);
                    html = en.GetString(content);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return html;
        }

        public byte[] GetBytes(string url, string postString = null, HttpHelperAttachInfo attachInfo = null)
            => GetBytes(url, postString?.ToBytes(), attachInfo);

        public byte[] GetBytes(string url, byte[] postData, HttpHelperAttachInfo attachInfo = null)
        {
            var request = GetRequest(url, postData, attachInfo);
            var httpWebResponse = GetResponse(request, attachInfo);

            var bs = GetBytes(httpWebResponse);
            httpWebResponse?.Close();
            request?.Abort();
            return bs;
        }

        public void GetBytes(string url, byte[] postData, GetBytesCallBack callBack, HttpHelperAttachInfo attachInfo = null)
        {
            var request = GetRequest(url, postData, attachInfo);
            var httpWebResponse = GetResponse(request, attachInfo);
            var responseStream = httpWebResponse?.GetResponseStream();
            try
            {
                if (responseStream != null)
                {
                    var bs = new byte[4096];
                    int readLen;
                    while ((readLen = responseStream.Read(bs, 0, bs.Length)) > 0)
                    {
                        callBack(bs, readLen);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                responseStream?.Close();
                httpWebResponse?.Close();
                request?.Abort();
            }
        }

        public Image GetImage(string url, HttpHelperAttachInfo attachInfo = null)
        {
            var request = GetRequest(url, null, attachInfo);//最后关闭用
            var response = GetResponse(request, attachInfo);

            var img = GetImage(response);
            response?.Close();
            request?.Abort();
            return img;
        }


        public string GetHtml(string url, string postString = null, HttpHelperAttachInfo attachInfo = null)
            => GetHtml(url, postString?.ToBytes(), attachInfo);

        public string GetHtml(string url, byte[] postData, HttpHelperAttachInfo attachInfo = null)
        {
            return GetHtml(GetBytes(url, postData, attachInfo));
        }

        public string GetHtml(string url, object postData, HttpHelperAttachInfo attachInfo = null)
        {
            ContentType = HttpContentType.Json;
            return GetHtml(GetBytes(url, Json.Encode(postData), attachInfo));
        }

        public string UploadFile(string url, Dictionary<string, object> forms, string file, string fileFormName,
            string uploadFileName = null, HttpHelperAttachInfo attachInfo = null)
            => UploadData(url, forms, File.ReadAllBytes(file), fileFormName, uploadFileName ?? Path.GetFileName(file), attachInfo);

        public string UploadData(string url, Dictionary<string, object> forms, byte[] data, string fileFormName,
            string uploadFileName = null, HttpHelperAttachInfo attachInfo = null)
        {
            var boundaryInfo = new BoundaryInfo();
            ContentType = boundaryInfo.ContentType;

            boundaryInfo.PacketForm(forms);

            boundaryInfo.PacketFormFile(data, fileFormName, uploadFileName);

            boundaryInfo.PacketFormEnd();

            return GetHtml(GetBytes(url, boundaryInfo.ToBytes(), attachInfo));
        }


        public string UploadForm(string url, Dictionary<string, object> forms, HttpHelperAttachInfo attachInfo = null)
        {
            var boundaryInfo = new BoundaryInfo();
            ContentType = boundaryInfo.ContentType;

            boundaryInfo.PacketForm(forms);

            boundaryInfo.PacketFormEnd();

            return GetHtml(GetBytes(url, boundaryInfo.ToBytes(), attachInfo));
        }

#if !net4
        private async Task<HttpWebResponse> GetResponseAsync(HttpWebRequest httpWebRequest,
            HttpHelperAttachInfo attachInfo)
        {
            var num = attachInfo?.ReGetNum > 0 ? attachInfo.ReGetNum : (ReGetNum > 0 ? ReGetNum : 1);
            for (var i = 0; i < num; i++)
            {
                if (httpWebRequest == null) return null;
                HttpWebResponse httpWebResponse = null;
                try
                {
                    httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                    ResponseHeader = httpWebResponse.Headers;
                    CharacterSet = httpWebResponse.CharacterSet;
                    GetAllCookies(httpWebRequest.CookieContainer);
                    return httpWebResponse;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    httpWebResponse?.Close();
                }
                Thread.Sleep(200 * (i + 1));
            }

            return null;
        }


        public Task<byte[]> GetBytesAsync(string url, string postString = null, HttpHelperAttachInfo attachInfo = null)
            => GetBytesAsync(url, postString?.ToBytes(), attachInfo);

        public async Task<byte[]> GetBytesAsync(string url, byte[] postData, HttpHelperAttachInfo attachInfo = null)
        {
            var request = GetRequest(url, postData, attachInfo);
            var httpWebResponse = await GetResponseAsync(request, attachInfo);

            var bs = GetBytes(httpWebResponse);
            httpWebResponse?.Close();
            request?.Abort();
            return bs;
        }

        public async void GetBytesAsync(string url, byte[] postData, GetBytesCallBack callBack, HttpHelperAttachInfo attachInfo = null)
        {
            var request = GetRequest(url, postData, attachInfo);
            var httpWebResponse = await GetResponseAsync(request, attachInfo);
            var responseStream = httpWebResponse?.GetResponseStream();
            try
            {
                if (responseStream != null)
                {
                    var bs = new byte[4096];
                    int readLen;
                    while ((readLen = responseStream.Read(bs, 0, bs.Length)) > 0)
                    {
                        callBack(bs, readLen);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                responseStream?.Close();
                httpWebResponse?.Close();
                request?.Abort();
            }
        }


        public Task<string> GetHtmlAsync(string url, string postString = null, HttpHelperAttachInfo attachInfo = null)
            => GetHtmlAsync(url, postString?.ToBytes(), attachInfo);

        public async Task<string> GetHtmlAsync(string url, byte[] postData, HttpHelperAttachInfo attachInfo = null)
        {
            return GetHtml(await GetBytesAsync(url, postData, attachInfo));
        }

        public async Task<string> GetHtmlAsync(string url, object postData, HttpHelperAttachInfo attachInfo = null)
        {
            ContentType = HttpContentType.Json;
            return GetHtml(await GetBytesAsync(url, Json.Encode(postData), attachInfo));
        }

        public async Task<Image> GetImageAsync(string url, HttpHelperAttachInfo attachInfo = null)
        {
            var request = GetRequest(url, null, attachInfo);//最后关闭用
            var response = await GetResponseAsync(request, attachInfo);

            var img = GetImage(response);
            response?.Close();
            request?.Abort();

            return img;
        }

        public Task<string> UploadFileAsync(string url, Dictionary<string, object> forms, string file, string fileFormName,
            string uploadFileName = null, HttpHelperAttachInfo attachInfo = null) => UploadDataAsync(url, forms, File.ReadAllBytes(file), fileFormName, uploadFileName ?? Path.GetFileName(file), attachInfo);

        public async Task<string> UploadDataAsync(string url, Dictionary<string, object> forms, byte[] data, string fileFormName,
            string uploadFileName = null, HttpHelperAttachInfo attachInfo = null)
        {
            var boundaryInfo = new BoundaryInfo();
            ContentType = boundaryInfo.ContentType;

            boundaryInfo.PacketForm(forms);

            boundaryInfo.PacketFormFile(data, fileFormName, uploadFileName);

            boundaryInfo.PacketFormEnd();

            return GetHtml(await GetBytesAsync(url, boundaryInfo.ToBytes(), attachInfo));
        }


        public async Task<string> UploadFormAsync(string url, Dictionary<string, object> forms, HttpHelperAttachInfo attachInfo = null)
        {
            var boundaryInfo = new BoundaryInfo();
            ContentType = boundaryInfo.ContentType;

            boundaryInfo.PacketForm(forms);

            boundaryInfo.PacketFormEnd();

            return GetHtml(await GetBytesAsync(url, boundaryInfo.ToBytes(), attachInfo));
        }

#endif
        private static bool CheckValidationResult(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) => true;
    }
}