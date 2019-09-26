using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Light.Ali.ApiGateway;
using Light.Ali.ApiGateway.SDK;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Light.Ali.ApiGatewayClient.Test
{
    [TestClass]
    public class UnitTest1
    {
        /// <summary>
        /// appKeyInfo
        /// </summary>
        private static readonly string AppKey = "24900000";

        private static readonly string AppSecret = "f0c9488dc4ed8ed0f34e7e3d62e40672";

        private static string baseUrl = "http://test.com/api/v1";

        private static HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        [TestMethod]
        public void TestGet()
        {
            var url = baseUrl + $"/latest/0?a=1&b=2";
            var httpMessage = new ApiGateWayHttpRequestMessage(HttpMethod.Get, url, AppKey, AppSecret);

            //1.first add custom headers
            var timeStamp = GetSecondTimeStamps(DateTime.Now);
            httpMessage.Headers.Add("timeStamp", timeStamp.ToString());
            //2.add the ali header
            httpMessage.AddAliHeader();
            //3.send the request
            var response = httpClient.SendAsync(httpMessage).Result;
            Assert.IsTrue(response.IsSuccessStatusCode);

        }

        [TestMethod]
        public void TestPostForm()
        {
            var url = baseUrl + $"/latest/0";
            var httpMessage = new ApiGateWayHttpRequestMessage(HttpMethod.Post, url, AppKey, AppSecret);

            //add custom headers
            var timeStamp = GetSecondTimeStamps(DateTime.Now);
            httpMessage.Headers.Add("timeStamp", timeStamp.ToString());

            //build content
            httpMessage.Content = new StringContent("a=1&b=2", Encoding.UTF8, "application/x-www-form-urlencoded");

            httpMessage.AddAliHeader();

            var response = httpClient.SendAsync(httpMessage).Result;
            Assert.IsTrue(response.IsSuccessStatusCode);

        }

        [TestMethod]
        public void TestPostJson()
        {
            var url = baseUrl + $"/latest/0";
            var httpMessage = new ApiGateWayHttpRequestMessage(HttpMethod.Post, url, AppKey, AppSecret);

            //add custom headers
            var timeStamp = GetSecondTimeStamps(DateTime.Now);
            httpMessage.Headers.Add("timeStamp", timeStamp.ToString());

            //build content
            httpMessage.Content = new StringContent("{'a':'1'}", Encoding.UTF8, "application/json");

            httpMessage.AddAliHeader();

            var response = httpClient.SendAsync(httpMessage).Result;
            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        public static long GetSecondTimeStamps(DateTime time)
        {
            DateTime start = new DateTime(1970, 1, 1);
            return (long)(time.ToUniversalTime() - start).TotalSeconds;
        }
    }
}
