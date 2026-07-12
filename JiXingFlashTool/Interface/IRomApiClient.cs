using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JiXingFlashTool.Interface
{
    /// <summary>
    /// ROM 服务接口客户端抽象，负责统一向刷机工具后端发送 HTTP 请求。
    /// </summary>
    public interface IRomApiClient
    {
        /// <summary>
        /// 当前编译环境对应的后端服务基地址。
        /// </summary>
        string BaseUrl { get; }

        /// <summary>
        /// 发送相对路径 HTTP 请求，并由调用方处理具体业务响应。
        /// </summary>
        /// <param name="relativePath">相对接口路径，不需要包含服务器基地址。</param>
        /// <param name="method">HTTP 请求方法。</param>
        /// <param name="content">请求内容，无请求体时传入 null。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>HTTP 响应消息。</returns>
        Task<HttpResponseMessage> SendAsync(string relativePath, HttpMethod method, HttpContent content, CancellationToken cancellationToken);

        /// <summary>
        /// 发送 ROM 后端请求并返回已解密的 data 明文。
        /// </summary>
        /// <param name="relativePath">接口相对路径，不需要包含服务器基础地址。</param>
        /// <param name="method">HTTP 请求方法。</param>
        /// <param name="content">请求内容，无请求体时传入 null。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> SendForDecryptedDataAsync(string relativePath, HttpMethod method, HttpContent content, CancellationToken cancellationToken);
    }
}
