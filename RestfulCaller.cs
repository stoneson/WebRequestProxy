
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace WebRequestProxy
{
    /// <summary>
    /// Restful Web Api调用
    /// </summary>
    public class RestfulCaller
    {
        #region 属性
        public static string BaseDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory.Substring(0, AppDomain.CurrentDomain.BaseDirectory.LastIndexOf('\\'));
                //return System.AppContext.BaseDirectory;
            }
        }
        /// <summary>
        /// 端点路径
        /// </summary>
        public string EndPoint { get; set; }

        /// <summary>
        /// 请求方式
        /// </summary>
        public RestSharp.Method Method { get; set; }

        /// <summary>
        /// 文本类型（1、application/json 2、txt/html）
        /// </summary>
        public string ContentType { get; set; }
        public RestSharp.DataFormat RequestFormat { get; set; } = RestSharp.DataFormat.Json;
        /// <summary>
        /// 请求的数据(一般为JSon格式)
        /// </summary>
        public string PostData { get; set; }

        ///// <summary>
        ///// 是否证书验证
        ///// </summary>
        //public static bool IsSsl { get; set; } = false;
        ///// <summary>
        ///// 验证证书文件
        ///// </summary>
        //public static string PfxFile { get; set; }
        ///// <summary>
        ///// 验证证书密码
        ///// </summary>
        //public static string Pfxkey { get; set; }
        #endregion

        #region 初始化
        public RestfulCaller()
        {
            EndPoint = "";
            Method = RestSharp.Method.GET;
            ContentType = "application/json";
            PostData = "";
        }

        public RestfulCaller(string endpoint)
        {
            EndPoint = endpoint;
            Method = RestSharp.Method.GET;
            ContentType = "application/json";
            PostData = "";
        }

        public RestfulCaller(string endpoint, RestSharp.Method method)
        {
            EndPoint = endpoint;
            Method = method;
            ContentType = "application/json";
            PostData = "";
        }

        public RestfulCaller(string endpoint, RestSharp.Method method, string postData)
        {
            EndPoint = endpoint;
            Method = method;
            ContentType = "application/json";
            PostData = postData;
        }
        #endregion

        #region HttpRequest 方法
        ///// <summary>
        ///// http请求(不带参数请求)
        ///// </summary>
        ///// <returns></returns>
        //public string HttpRequest()
        //{
        //    return HttpRequest("");
        //}

        ///// <summary>
        ///// http请求(带参数)
        ///// </summary>
        ///// <param name="parameters">parameters例如：?name=LiLei</param>
        ///// <returns></returns>
        //public string HttpRequest(string parameters)
        //{
        //    return HttpRequest(Method, EndPoint + parameters, PostData, ContentType);
        //}
        #endregion

        #region static HttpRequest GET/POST/PUT/DELETE
        /// <summary>
        /// Http (GET)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static string HttpRequestGET(string url//, string contentType = "application/json"
            , IDictionary<string, string> parameters = null, IDictionary<string, string> headers = null, RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            return HttpRequest(RestSharp.Method.GET, url, "", parameters, headers, requestFormat, webProxyAddress);
        }
        /// <summary>
        /// Http (POST)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static string HttpRequestPOST(string url, string postData//, string contentType = "application/json"
            , IDictionary<string, string> headers = null, RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            return HttpRequest(RestSharp.Method.POST, url, postData, null, headers, requestFormat, webProxyAddress);
        }
        /// <summary>
        /// Http (PUT)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">TTP 标头的值</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static string HttpRequestPUT(string url, string postData//, string contentType = "application/json"
            , IDictionary<string, string> headers = null, RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            return HttpRequest(RestSharp.Method.PUT, url, postData, null, headers, requestFormat, webProxyAddress);
        }
        /// <summary>
        ///  Http (DELETE)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static string HttpRequestDELETE(string url, string postData//, string contentType = "application/json"
            , IDictionary<string, string> headers = null, RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            return HttpRequest(RestSharp.Method.DELETE, url, postData, null, headers, requestFormat, webProxyAddress);
        }
        /// <summary>
        /// Http (GET/POST/PUT/DELETE)
        /// </summary>
        /// <param name="method">请求方法</param>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static string HttpRequest(RestSharp.Method method, string url, string postData//, string contentType = "application/json"
            , IDictionary<string, string> parameters = null, IDictionary<string, string> headers = null
            , RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            //设置请求参数
            if (parameters != null && parameters.Count > 0)
            {
                url += (url.Contains("?") ? "&" : "?") + BuildQuery(parameters, Encoding.UTF8);
            }
            var request = (HttpWebRequest)WebRequest.Create(url);
            //设置HTTP 标头的名称/值对的集合
            if (headers != null && headers.Count > 0)
            {
                BuildHeader(request, headers);
            }
            //设置代理
            if (!string.IsNullOrEmpty(webProxyAddress))
            {
                var proxy = new WebProxy(webProxyAddress);//IP地址 port为端口号 代理类
                request.Proxy = proxy;
            }
            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = method.ToString();
            request.ContentLength = 0;
            //request.ContentType = contentType;
            if (requestFormat == RestSharp.DataFormat.Json)
            {
                request.ContentType = "application/json";
            }
            else if (requestFormat == RestSharp.DataFormat.Xml)
            {
                request.ContentType = "application/xml";
            }
            else
            {
                request.ContentType = "application/text";
            }
            //var fin = Path.Combine(System.Windows.Forms.Application.StartupPath, "serviceHost.com.pfx");
            //if (System.IO.File.Exists(fin))
            //{
            //    var cate = GetX509Certificate(fin, "password");
            //    request.ClientCertificates.Add(cate);
            //}
            //--------------------------------SSL---------------------------------------------
            //if (IsSsl && !string.IsNullOrWhiteSpace(PfxFile))
            //{
            //    var cerCaiShang = new X509Certificate(PfxFile, Pfxkey);
            //    request.ClientCertificates.Add(cerCaiShang);
            //}
            //-----------------------------------------------------------------------------
            if (!string.IsNullOrEmpty(postData) && method != RestSharp.Method.GET)
            {
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(postData);
                    request.ContentLength = bytes.Length;

                    //创建输入流
                    using (var writeStream = request.GetRequestStream())
                    {
                        writeStream.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    return ex.ToString();//连接服务器失败
                }
            }
            //-------------------------读取返回消息----------------------------------------------------------------------
            return GetResponseAsString(request, Encoding.UTF8);
        }
        static void BuildHeader(HttpWebRequest request, IDictionary<string, string> headers)
        {
            if (request == null) return;
            IEnumerator<KeyValuePair<string, string>> dem = headers.GetEnumerator();
            while (dem.MoveNext())
            {
                string name = dem.Current.Key;
                string value = dem.Current.Value;
                // 忽略参数名或参数值为空的参数
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                {
                    request.Headers[name] = value;
                }
            }
        }
        /// <summary>
        /// 组装普通文本请求参数。
        /// </summary>
        /// <param name="parameters">Key-Value形式请求参数字典</param>
        /// <returns>URL编码后的请求数据</returns>
        static string BuildQuery(IDictionary<string, string> parameters, Encoding encode = null)
        {
            StringBuilder postData = new StringBuilder();
            bool hasParam = false;
            IEnumerator<KeyValuePair<string, string>> dem = parameters.GetEnumerator();
            while (dem.MoveNext())
            {
                string name = dem.Current.Key;
                string value = dem.Current.Value;
                // 忽略参数名或参数值为空的参数
                if (!string.IsNullOrEmpty(name))//&& !string.IsNullOrEmpty(value)
                {
                    if (hasParam)
                    {
                        postData.Append("&");
                    }
                    postData.Append(name);
                    postData.Append("=");
                    if (encode == null)
                    {
                        postData.Append(System.Web.HttpUtility.UrlEncode(value, encode));
                    }
                    else
                    {
                        postData.Append(value);
                    }
                    hasParam = true;
                }
            }
            return postData.ToString();
        }

        /// <summary>
        /// 把响应流转换为文本。
        /// </summary>
        /// <param name="rsp">响应流对象</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>响应文本</returns>
        static string GetResponseAsString(HttpWebRequest request, Encoding encoding)
        {
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                return ex.Message;
                //response = (HttpWebResponse)ex.Response;
            }
            catch (Exception ex)
            {
                return ex.ToString();
                //连接服务器失败
            }
            //读取返回消息
            string res = string.Empty;
            try
            {
                //判断HTTP响应状态 
                if (response == null || response.StatusCode != HttpStatusCode.OK)
                {
                    response.Close();
                    res = " 访问失败:Response.StatusCode=" + response.StatusCode;//连接服务器失败
                }
                else
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream(), encoding);
                    res = reader.ReadToEnd();
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                res = ex.ToString();
                //连接服务器失败
            }
            return res;
        }
        #endregion

        #region RestSharpRequest 方法
        /// <summary>
        /// http RestSharp 请求(不带参数请求)
        /// </summary>
        /// <returns></returns>
        public object RestSharpRequest()
        {
            return RestSharpRequest("");
        }

        /// <summary>
        /// http RestSharp请求(带参数)
        /// </summary>
        /// <param name="parameters">parameters例如：?name=LiLei</param>
        /// <returns></returns>
        public object RestSharpRequest(string parameters)
        {
            return RestSharpRequest(Method, EndPoint + parameters, PostData, null, null, RequestFormat);
        }
        public static string GetQueryString(IDictionary<string, string> parameters, Encoding encode = null)
        {
            StringBuilder postData = new StringBuilder();
            bool hasParam = false;
            IEnumerator<KeyValuePair<string, string>> dem = parameters.GetEnumerator();
            while (dem.MoveNext())
            {
                string name = dem.Current.Key;
                string value = dem.Current.Value;
                // 忽略参数名或参数值为空的参数
                if (!string.IsNullOrEmpty(name))//&& !string.IsNullOrEmpty(value)
                {
                    if (hasParam)
                    {
                        postData.Append("&");
                    }
                    postData.Append(name);
                    postData.Append("=");
                    if (encode == null)
                    {
                        postData.Append(System.Web.HttpUtility.UrlEncode(value, encode));
                    }
                    else
                    {
                        postData.Append(value);
                    }
                    hasParam = true;
                }
            }
            return postData.ToString();
        }
        #endregion

        #region static RestSharpRequest GET/POST/PUT/DELETE
        /// <summary>
        /// Http RestSharp(GET)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static object RestSharpGET(string url, IDictionary<string, string> parameters = null, IDictionary<string, string> headers = null
            , RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            return RestSharpRequest(RestSharp.Method.GET, url, "", parameters, headers, requestFormat, webProxyAddress);
        }
        /// <summary>
        /// Http RestSharp(POST)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static object RestSharpPOST(string url, object postData, IDictionary<string, string> headers = null
            , RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            return RestSharpRequest(RestSharp.Method.POST, url, postData, null, headers, requestFormat, webProxyAddress);
        }
        /// <summary>
        /// Http RestSharp(PUT)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">TTP 标头的值</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static object RestSharpPUT(string url, object postData, IDictionary<string, string> headers = null
            , RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            return RestSharpRequest(RestSharp.Method.PUT, url, postData, null, headers, requestFormat, webProxyAddress);
        }
        /// <summary>
        ///  Http (DELETE)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static object RestSharpDELETE(string url, object postData, IDictionary<string, string> headers = null
            , RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            return RestSharpRequest(RestSharp.Method.DELETE, url, postData, null, headers, requestFormat, webProxyAddress);
        }
        /// <summary>
        /// Http RestSharp(GET/POST/PUT/DELETE)
        /// </summary>
        /// <param name="method">请求方法</param>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static object RestSharpRequest(RestSharp.Method httpMethod, string url, object postData
            , IDictionary<string, string> parameters = null, IDictionary<string, string> headers = null
            , RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            try
            {
                var request = getRestSharpRequest(httpMethod, url, postData, parameters, headers, requestFormat, webProxyAddress);

                var restclient = getRestClient(webProxyAddress);
                var ret = restclient.Execute(request);
                //-------------------------读取返回消息----------------------------------------------------------------------
                return RestSharpRequestResponse(ret);
            }
            catch (Exception ex)
            {
                throw new Exception($"访问地址{url}失败,{ex.Message}");
            }
        }
        /// <summary>
        /// 异步 Http RestSharp(GET/POST/PUT/DELETE)
        /// </summary>
        /// <param name="method">请求方法</param>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static object RestSharpRequestAsync(RestSharp.Method httpMethod, string url, object postData
            , IDictionary<string, string> parameters = null, IDictionary<string, string> headers = null
            , RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            try
            {
                var request = getRestSharpRequest(httpMethod, url, postData, parameters, headers, requestFormat, webProxyAddress);

                var restclient = getRestClient(webProxyAddress);
                //var tcs = new TaskCompletionSource<object>();
                var ret = restclient.ExecuteAsync(request);
                //tcs.SetResult(RestSharpRequestResponse(ret.Result));
                return RestSharpRequestResponse(ret.Result);
            }
            catch (Exception ex)
            {
                throw new Exception($"访问地址{url}失败,{ex.Message}");
            }
        }

        static RestSharp.RestClient getRestClient(string webProxyAddress = "")
        {
            var restclient = new RestSharp.RestClient();
            restclient.RemoteCertificateValidationCallback += ValidateServerCertificate;
            //设置代理
            if (!string.IsNullOrEmpty(webProxyAddress))
            {
                restclient.Proxy = new WebProxy(webProxyAddress);//IP地址 port为端口号 代理类
            }
            return restclient;
        }

        static RestSharp.RestRequest getRestSharpRequest(RestSharp.Method httpMethod, string url, object postData
            , IDictionary<string, string> parameters = null, IDictionary<string, string> headers = null
            , RestSharp.DataFormat requestFormat = RestSharp.DataFormat.Json, string webProxyAddress = "")
        {
            //设置请求参数
            if (parameters != null && parameters.Count > 0)
            {
                url += (url.Contains("?") ? "&" : "?") + BuildQuery(parameters, Encoding.UTF8);
            }
            var request = new RestSharp.RestRequest(url, httpMethod);
            //request.RequestFormat = requestFormat;
            // request.JsonSerializer = new RestSharp.Serializers.Utf8Json.Utf8JsonSerializer();
            //request.Timeout = 10000;
            //设置HTTP 标头的名称/值对的集合
            if (headers != null && headers.Count > 0)
            {
                BuildHeader(request, headers);
            }

            //-----------------------------------------------------------------------------
            if (postData != null && !string.IsNullOrEmpty(postData.ToString()) && httpMethod != RestSharp.Method.GET)
            {
                if (requestFormat == RestSharp.DataFormat.Json)
                {
                    //request.AddJsonBody(postData);
                    request.AddParameter("application/json; charset=utf-8"
                        , postData is string ? postData.ToString() : Newtonsoft.Json.JsonConvert.SerializeObject(postData)
                        , ParameterType.RequestBody);
                }
                else if (requestFormat == RestSharp.DataFormat.Xml)
                {
                    request.AddXmlBody(postData);
                }
                else
                {
                    request.AddObject(postData);
                }
            }
            return request;
        }

        /// <summary>
        /// 读取返回消息
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        static object RestSharpRequestResponse(RestSharp.IRestResponse ret)
        {
            var response = ret.Content;
            //if (ret.RawBytes?.Length > 0)
            //{
            //    response = System.Text.UTF8Encoding.UTF8.GetString(ret.RawBytes);
            //}
            if (string.IsNullOrWhiteSpace(response) && response.Length < 2)
            {
                if (ret.ErrorException != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
                {
                    throw ret.ErrorException;
                }
                else
                {
                    return null;
                }
            }
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject(response.Substring(1));
            }
            catch
            {
                try
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject(response);
                }
                catch { }
            }
            if (ret.StatusCode == HttpStatusCode.OK)
            {
                return ret.Content;
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject("{\"code\":" + ret.StatusCode.GetHashCode() + ",\"msg\":\"" + ret.StatusDescription + "\"}");
            }
        }

        static void BuildHeader(RestSharp.RestRequest request, IDictionary<string, string> headers)
        {
            if (request == null) return;
            IEnumerator<KeyValuePair<string, string>> dem = headers.GetEnumerator();
            while (dem.MoveNext())
            {
                string name = dem.Current.Key;
                string value = dem.Current.Value;
                // 忽略参数名或参数值为空的参数
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                {
                    request.AddHeader(name, value);
                }
            }
        }
        #endregion

        #region SetCertificatePolicy
        static RestfulCaller()
        {
            SetCertificatePolicy();
        }
        /// <summary>
        /// Sets the cert policy.
        /// </summary>
        public static void SetCertificatePolicy()
        {
            //添加验证证书的回调方法
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            // 这里设置了协议类型。
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;// (SecurityProtocolType)3072;// SecurityProtocolType.Tls1.2; 
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            //ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3; //SecurityProtocolType.Tls;// (SecurityProtocolType)3072; // 
            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.Expect100Continue = false;
        }

        /// <summary>
        /// Remotes the certificate validate.
        /// </summary>
        private static bool ValidateServerCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            return true;
        }
        private static System.Security.Cryptography.X509Certificates.X509Certificate2 GetX509Certificate(string fin, string password = "password")
        {
            return new System.Security.Cryptography.X509Certificates.X509Certificate2(fin, password);
            //var store = new System.Security.Cryptography.X509Certificates.X509Store(System.Security.Cryptography.X509Certificates.StoreName.My, System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine);
            //store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.OpenExistingOnly);
            //System.Security.Cryptography.X509Certificates.X509Certificate certificate = null;
            //var cers = store.Certificates.Find(System.Security.Cryptography.X509Certificates.X509FindType.FindBySubjectName, "localhost", false);
            //if (cers.Count > 0)
            //{
            //    certificate = cers[0];
            //}
            //store.Close();
            //return certificate;
        }
        #endregion

        #region 读/写文件
        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="path">E:\\test.txt</param>
        /// <returns></returns>
        public static string Read(string path)
        {
            var rtStr = "";
            try
            {
                var pat = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(pat)) System.IO.Directory.CreateDirectory(pat);
                StreamReader sr = new StreamReader(path, Encoding.Default);
                System.String line;
                while ((line = sr.ReadLine()) != null)
                {
                    rtStr += line.ToString();
                }
            }
            catch (IOException e)
            {
                rtStr = e.ToString();
            }
            return rtStr;
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        public static void Write(string path, string content)
        {
            var pat = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(pat)) System.IO.Directory.CreateDirectory(pat);
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            //开始写入
            sw.Write(content);
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();
        }
        #endregion
    }
}