
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using Light.Ali.ApiGateway.SDK.Constants;

namespace Light.Ali.ApiGateway.SDK
{
    /// <summary>Class AliGatewayHttpRequestMessage.</summary>
    /// <seealso cref="System.Net.Http.HttpRequestMessage"/>
    public class ApiGateWayHttpRequestMessage : HttpRequestMessage
    {
        #region Fileds

        /// <summary>
        /// 表单类型Content-Type
        /// </summary>
        private const string ContentTypeForm = "application/x-www-form-urlencoded; charset=utf-8";

        /// <summary>
        /// JSON类型Content-Type
        /// </summary>
        private const string ContentTypeJson = "application/json; charset=utf-8";
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiGateWayHttpRequestMessage" /> class.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="appKey">The application key.</param>
        /// <param name="appSecret">The application secret.</param>
        /// <param name="signHeaderPrefixList">The sign header prefix list.</param>
        /// <param name="isDebug">if set to <c>true</c> [is debug].</param>
        public ApiGateWayHttpRequestMessage(HttpMethod method, string requestUri, string appKey, string appSecret, List<string> signHeaderPrefixList = null, bool isDebug = false) : base(method, requestUri)
        {
            AppKey = appKey;
            AppSecret = appSecret;
            SignHeaderPrefixList = signHeaderPrefixList ?? new List<string> { XCaHeader.XCaTimestamp };
            IsDebug = isDebug;
        }

        #region Props

        /// <summary>
        /// 是否开启 Debug 模式，大小写不敏感，不设置默认关闭，一般 API 调试阶段可以打开此设置。
        /// </summary>
        /// <value><c>true</c> if this instance is debug; otherwise, <c>false</c>.</value>
        private bool IsDebug { get; set; }

        /// <summary>
        /// 请求的 AppKey，请到 API 网关控制台生成，只有获得 API 授权后才可以调用，
        /// 通过云市场等渠道购买的 API 默认已经给APP授过权，阿里云所有云产品共用一套 AppKey 体系，
        /// 删除 AppKey 请谨慎，避免影响到其他已经开通服务的云产品。
        /// </summary>
        /// <value>The application key.</value>
        private string AppKey { get; set; }

        /// <summary>
        /// 请到 API 网关控制台生成，
        /// </summary>
        /// <value>The application secret.</value>
        private string AppSecret { get; set; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        private string Path => this.RequestUri.AbsolutePath;

        /// <summary>
        /// Gets the query dic.
        /// </summary>
        /// <value>The query dic.</value>
        private Dictionary<string, string> QueryDic => GetDicFormQuery();

        /// <summary>
        /// 参与签名的自定义请求头，服务端将根据此配置读取请求头进行签名，
        /// 此处设置不包含 Content-Type、Accept、Content-MD5、Date 请求头，这些请求头已经包含在了基础的签名结构中，
        /// 详情参照请求签名说明文档。
        /// </summary>
        /// <value>The signature headers.</value>
        private List<string> SignHeaderPrefixList { get; set; }
        #endregion

        /// <summary>Initializes the ali header.</summary>
        public void AddAliHeader()
        {
            var aliHeaders = CreateAliHeader();
            foreach (var pair in aliHeaders.Where(x => !this.Headers.Contains(x.Key)))
            {
                this.Headers.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Creates the ali yun header.
        /// </summary>
        /// <returns>Dictionary&lt;System.String, System.String&gt;.</returns>
        private Dictionary<string, string> CreateAliHeader()
        {
            var headerDic = GetDicFormHeader();
            var bodyDic = new Dictionary<string, string>();
            if (IsDebug)
            {
                headerDic.Add("X-Ca-Request-Mode", "debug");
            }

            headerDic.Add(XCaHeader.XCaKey, AppKey);
            //设定Accept，根据服务器端接受的值来设置
            headerDic.Add(HttpHeader.HttpHeaderAccept, ContentTypeJson);
            //可选：请求的时间戳，值为当前时间的毫秒数，也就是从1970年1月1日起至今的时间转换为毫秒，时间戳有效时间为15分钟。
            headerDic.Add(XCaHeader.XCaTimestamp, GetCurrentTimeStamps());
            //可选：防重放，协议层不能进行重试，否则会报NONCE被使用；如果需要协议层重试，请注释此行
            headerDic.Add(XCaHeader.XCaNonce, Guid.NewGuid().ToString());
            if (this.Content != null)
            {
                var bodyStr = this.Content.ReadAsStringAsync().Result;
                var contentType = this.Content.Headers.ContentType.ToString();
                if (contentType == ContentTypeForm)
                {
                    //当提交的是form类型，需要把body拼接到url中
                    bodyDic = GetBodyDic(bodyStr);
                    headerDic.Add(HttpHeader.HttpHeaderContentType, ContentTypeForm);
                }
                else
                {
                    //可选：Content-MD5 是指 Body 的 MD5 值，只有当 Body 非 Form 表单时才计算 MD5，计算方式为：Base64.encodeBase64(MD5(bodyStream.getbytes("UTF-8")));
                    headerDic.Add(HttpHeader.HttpHeaderContentMd5, SignUtil.Base64AndMD5(Encoding.UTF8.GetBytes(bodyStr)));
                    headerDic.Add(HttpHeader.HttpHeaderContentType, ContentTypeJson);
                }
            }

            //请求签名
            headerDic.Add(XCaHeader.XCaSignature, SignUtil.Sign(Path, this.Method.ToString(), AppSecret, headerDic, QueryDic, bodyDic, SignHeaderPrefixList));
            return headerDic;
        }

        /// <summary>
        /// Gets the dic form query.
        /// </summary>
        /// <returns>Dictionary&lt;System.String, System.String&gt;.</returns>
        private Dictionary<string, string> GetDicFormQuery()
        {
            var nvc = HttpUtility.ParseQueryString(this.RequestUri.Query);
            return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
        }


        /// <summary>
        /// Gets the dic form header.
        /// </summary>
        /// <returns>Dictionary&lt;System.String, System.String&gt;.</returns>
        private Dictionary<string, string> GetDicFormHeader()
        {
            return this.Headers.ToDictionary(x => x.Key, x => string.Join(",", x.Value));
        }

        /// <summary>
        /// Gets the body dic.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <returns>Dictionary&lt;System.String, System.String&gt;.</returns>
        private Dictionary<string, string> GetBodyDic(string body)
        {
            var nvc = HttpUtility.ParseQueryString(body);
            return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
        }

        /// <summary>
        /// 将c# DateTime时间格式转换为Unix时间戳格式 13位（毫秒为单位）
        /// </summary>
        /// <returns>long</returns>
        private static string GetCurrentTimeStamps()
        {
            DateTime start = new DateTime(1970, 1, 1);
            return ((long)(DateTime.UtcNow - start).TotalMilliseconds).ToString();
        }
    }
}
