using System.Runtime.Serialization;

namespace JiXingFlashTool.Model.RomApi
{
    /// <summary>
    /// ROM 后端通用加密响应模型，承载服务端返回的状态、消息和加密 data 字段。
    /// </summary>
    [DataContract]
    public sealed class RomApiEncryptedResponse
    {
        /// <summary>
        /// 接口返回的业务状态码。
        /// </summary>
        [DataMember(Name = "code")]
        public int Code { get; set; }

        /// <summary>
        /// 接口返回的业务提示消息。
        /// </summary>
        [DataMember(Name = "message")]
        public string Message { get; set; }

        /// <summary>
        /// 兼容部分接口使用 msg 字段返回提示消息。
        /// </summary>
        [DataMember(Name = "msg")]
        public string Msg { get; set; }

        /// <summary>
        /// 接口返回的加密业务数据。
        /// </summary>
        [DataMember(Name = "data")]
        public string Data { get; set; }

        /// <summary>
        /// 获取优先可展示的接口提示消息。
        /// </summary>
        /// <returns>接口消息文本。</returns>
        public string GetDisplayMessage()
        {
            return string.IsNullOrWhiteSpace(Message) ? Msg : Message;
        }
    }
}
