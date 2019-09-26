using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Light.Ali.ApiGateway.SDK.Constants;

namespace Light.Ali.ApiGateway.SDK
{
    /// <summary>
    /// 阿里云API网关签名类
    /// </summary>
    public partial class SignUtil
    {
        /// <summary>Sign</summary>
        /// <param name="path">path</param>
        /// <param name="method">method</param>
        /// <param name="secret">secret</param>
        /// <param name="headers">headers</param>
        /// <param name="querys">querys</param>
        /// <param name="bodys">bodys</param>
        /// <param name="signHeaderPrefixList">signHeaderPrefixList</param>
        /// <returns>string</returns>
        public static string Sign(string path, string method, string secret, Dictionary<string, string> headers, Dictionary<string, string> querys, Dictionary<string, string> bodys, List<string> signHeaderPrefixList)
        {
            using (var algorithm = KeyedHashAlgorithm.Create("HMACSHA256"))
            {
                algorithm.Key = Encoding.UTF8.GetBytes(secret.ToCharArray());
                string signStr = BuildStringToSign(path, method, headers, querys, bodys, signHeaderPrefixList);
                return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(signStr.ToCharArray())));
            }
        }

        /// <summary>
        /// BuildStringToSign
        /// </summary>
        /// <param name="path">path</param>
        /// <param name="method">method</param>
        /// <param name="headers">headers</param>
        /// <param name="querys">querys</param>
        /// <param name="bodys">bodys</param>
        /// <param name="signHeaderPrefixList">signHeaderPrefixList</param>
        /// <returns>string</returns>
        private static string BuildStringToSign(string path, string method, Dictionary<string, string> headers, Dictionary<string, string> querys, Dictionary<string, string> bodys, List<string> signHeaderPrefixList)
        {
            string lf = "\n";

            var sb = new StringBuilder();

            sb.Append(method.ToUpper()).Append(lf);
            if (headers.ContainsKey(HttpHeader.HttpHeaderAccept) && headers[HttpHeader.HttpHeaderAccept] != null)
            {
                sb.Append(headers[HttpHeader.HttpHeaderAccept]);
            }
            sb.Append(lf);
            if (headers.ContainsKey(HttpHeader.HttpHeaderContentMd5) && headers[HttpHeader.HttpHeaderContentMd5] != null)
            {
                sb.Append(headers[HttpHeader.HttpHeaderContentMd5]);
            }
            sb.Append(lf);
            if (headers.ContainsKey(HttpHeader.HttpHeaderContentType) && headers[HttpHeader.HttpHeaderContentType] != null)
            {
                sb.Append(headers[HttpHeader.HttpHeaderContentType]);
            }
            sb.Append(lf);
            if (headers.ContainsKey(HttpHeader.HttpHeaderDate) && headers[HttpHeader.HttpHeaderDate] != null)
            {
                sb.Append(headers[HttpHeader.HttpHeaderDate]);
            }
            sb.Append(lf);
            sb.Append(BuildHeaders(headers, signHeaderPrefixList));
            sb.Append(BuildResource(path, querys, bodys));

            return sb.ToString();
        }

        /// <summary>
        /// 构建待签名Path+Query+FormParams
        /// </summary>
        /// <param name="path">path</param>
        /// <param name="querys">querys</param>
        /// <param name="bodys">bodys</param>
        /// <returns>string</returns>
        private static string BuildResource(string path, Dictionary<string, string> querys, Dictionary<string, string> bodys)
        {
            StringBuilder sb = new StringBuilder();
            if (path != null)
            {
                sb.Append(path);
            }
            var sbParam = new StringBuilder();
            IDictionary<string, string> sortParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
            //query参与签名
            if (querys != null && querys.Count > 0)
            {
                foreach (var param in querys)
                {
                    if (param.Key.Length > 0)
                    {
                        sortParams.Add(param.Key, param.Value);
                    }
                }
            }

            //body参与签名
            if (bodys != null && bodys.Count > 0)
            {
                foreach (var param in bodys)
                {
                    if (param.Key.Length > 0)
                    {
                        sortParams.Add(param.Key, param.Value);
                    }
                }
            }
            //参数Key           
            foreach (var param in sortParams)
            {
                if (param.Key.Length > 0)
                {
                    if (sbParam.Length > 0)
                    {
                        sbParam.Append("&");
                    }
                    sbParam.Append(param.Key);
                    if (!string.IsNullOrEmpty(param.Value))
                    {
                        sbParam.Append("=").Append(param.Value);
                    }
                }
            }
            if (sbParam.Length > 0)
            {
                sb.Append("?").Append(sbParam);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 构建待签名Http头
        /// </summary>
        /// <param name="headers">请求中所有的Http头</param>
        /// <param name="signHeaderPrefixList">自定义参与签名Header前缀</param>
        /// <returns>待签名Http头</returns>
        private static string BuildHeaders(Dictionary<string, string> headers, List<string> signHeaderPrefixList)
        {
            StringBuilder sb = new StringBuilder();

            if (signHeaderPrefixList != null)
            {
                //剔除X-Ca-Signature/X-Ca-Signature-Headers/Accept/Content-MD5/Content-Type/Date
                signHeaderPrefixList.Remove("X-Ca-Signature");
                signHeaderPrefixList.Remove("X-Ca-Signature-Headers");
                signHeaderPrefixList.Remove("Accept");
                signHeaderPrefixList.Remove("Content-MD5");
                signHeaderPrefixList.Remove("Content-Type");
                signHeaderPrefixList.Remove("Date");
                signHeaderPrefixList.Sort(StringComparer.Ordinal);
            }

            //Dictionary<String, String> headersToSign = new Dictionary<String, String>();            
            if (headers != null)
            {
                IDictionary<string, string> sortedParams = new SortedDictionary<string, string>(headers, StringComparer.Ordinal);
                StringBuilder signHeadersStringBuilder = new StringBuilder();

                foreach (var param in sortedParams)
                {
                    if (IsHeaderToSign(param.Key, signHeaderPrefixList))
                    {
                        sb.Append(param.Key).Append(":");
                        if (param.Value != null)
                        {
                            sb.Append(param.Value);
                        }
                        sb.Append("\n");
                        if (signHeadersStringBuilder.Length > 0)
                        {
                            signHeadersStringBuilder.Append(",");
                        }
                        signHeadersStringBuilder.Append(param.Key);
                    }
                }

                headers.Add(XCaHeader.XCaSignatureHeaders, signHeadersStringBuilder.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Http头是否参与签名
        /// </summary>
        /// <param name="headerName">headerName</param>
        /// <param name="signHeaderPrefixList">signHeaderPrefixList</param>
        /// <returns>bool</returns>
        private static bool IsHeaderToSign(string headerName, List<string> signHeaderPrefixList)
        {
            if (string.IsNullOrEmpty(headerName))
            {
                return false;
            }

            if (headerName.StartsWith("X-Ca-"))
            {
                return true;
            }

            if (signHeaderPrefixList != null)
            {
                foreach (var signHeaderPrefix in signHeaderPrefixList)
                {
                    if (headerName.StartsWith(signHeaderPrefix))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Base64AndMD5
        /// </summary>
        /// <param name="input">input</param>
        /// <returns>string</returns>
        public static string Base64AndMD5(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new Exception("input can not be null");
            }
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            return Base64AndMD5(bytes);
        }

        /// <summary>
        /// Base64AndMD5
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>string</returns>
        public static string Base64AndMD5(byte[] bytes)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = md5.ComputeHash(bytes);
            return Convert.ToBase64String(data);
        }
    }
}
