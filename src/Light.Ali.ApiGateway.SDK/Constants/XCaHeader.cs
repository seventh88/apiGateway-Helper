namespace Light.Ali.ApiGateway.SDK.Constants
{
    public class XCaHeader
    {
        /// <summary>
        /// 签名Header
        /// </summary>
        public const string XCaSignature = "X-Ca-Signature";

        /// <summary>
        /// 所有参与签名的Header
        /// </summary>
        public const string XCaSignatureHeaders = "X-Ca-Signature-Headers";

        /// <summary>
        /// 请求时间戳
        /// </summary>
        public const string XCaTimestamp = "X-Ca-Timestamp";

        /// <summary>
        /// 请求放重放Nonce,15分钟内保持唯一,建议使用UUID
        /// </summary>
        public const string XCaNonce = "X-Ca-Nonce";

        /// <summary>
        /// APP KEY
        /// </summary>
        public const string XCaKey = "X-Ca-Key";

        /// <summary>
        /// 请求API所属Stage
        /// </summary>
        public const string XCaStage = "X-Ca-Stage";

        /// <summary>
        /// debug
        /// </summary>
        public const string XCaDebug = "X-Ca-Request-Mode";
    }
}