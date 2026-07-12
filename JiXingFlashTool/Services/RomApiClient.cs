using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.RomApi;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JiXingFlashTool.Services
{
    /// <summary>
    /// ROM 后端 HTTP 客户端，统一封装刷机工具接口的服务器地址、超时和请求发送逻辑。
    /// </summary>
    public sealed class RomApiClient : IRomApiClient, IDisposable
    {
        private static readonly Lazy<RomApiClient> LazyInstance = new Lazy<RomApiClient>(() => new RomApiClient());
        private static readonly DataContractJsonSerializer ResponseSerializer = new DataContractJsonSerializer(typeof(RomApiEncryptedResponse));
        private readonly HttpClient _httpClient;
        private readonly RomApiDataDecryptor _dataDecryptor;

        /// <summary>
        /// 初始化 ROM 后端 HTTP 客户端。
        /// </summary>
        public RomApiClient()
            : this(new HttpClient())
        {
        }

        /// <summary>
        /// 使用指定 HttpClient 初始化 ROM 后端客户端，便于后续测试或扩展。
        /// </summary>
        /// <param name="httpClient">HTTP 客户端实例。</param>
        public RomApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _dataDecryptor = RomApiDataDecryptor.Instance;
            BaseUrl = RomApiEndpointProvider.GetBaseUrl();
            _httpClient.BaseAddress = new Uri(BaseUrl.TrimEnd('/') + "/");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 全局 ROM 后端客户端实例。
        /// </summary>
        public static RomApiClient Instance => LazyInstance.Value;

        /// <summary>
        /// 当前编译环境对应的后端服务基地址。
        /// </summary>
        public string BaseUrl { get; }

        /// <summary>
        /// 发送相对路径 HTTP 请求，并由调用方处理具体业务响应。
        /// </summary>
        /// <param name="relativePath">相对接口路径，不需要包含服务器基地址。</param>
        /// <param name="method">HTTP 请求方法。</param>
        /// <param name="content">请求内容，无请求体时传入 null。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>HTTP 响应消息。</returns>
        public Task<HttpResponseMessage> SendAsync(string relativePath, HttpMethod method, HttpContent content, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("接口路径不能为空。", nameof(relativePath));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var request = new HttpRequestMessage(method, NormalizeRelativePath(relativePath))
            {
                Content = content
            };

            return _httpClient.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// 发送 ROM 后端请求并返回已解密的 data 明文。
        /// </summary>
        /// <param name="relativePath">接口相对路径，不需要包含服务器基础地址。</param>
        /// <param name="method">HTTP 请求方法。</param>
        /// <param name="content">请求内容，无请求体时传入 null。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public async Task<string> SendForDecryptedDataAsync(string relativePath, HttpMethod method, HttpContent content, CancellationToken cancellationToken)
        {
            using (var response = await SendAsync(relativePath, method, content, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var encryptedResponse = DeserializeEncryptedResponse(responseText);

                if (encryptedResponse == null)
                {
                    throw new InvalidOperationException("ROM 接口响应格式无效。");
                }

                if (string.IsNullOrWhiteSpace(encryptedResponse.Data))
                {
                    throw new InvalidOperationException("ROM 接口响应缺少加密 data 字段。");
                }

                return _dataDecryptor.DecryptData(encryptedResponse.Data);
            }
        }

        /// <summary>
        /// 释放 HTTP 客户端资源。
        /// </summary>
        public void Dispose()
        {
            _httpClient.Dispose();
        }

        /// <summary>
        /// 标准化接口相对路径，避免调用方传入以斜杠开头时覆盖 BaseAddress 的 /client 路径。
        /// </summary>
        /// <param name="relativePath">调用方传入的接口相对路径。</param>
        /// <returns>可安全拼接到 BaseAddress 的相对路径。</returns>
        private static string NormalizeRelativePath(string relativePath)
        {
            return relativePath.TrimStart('/');
        }

        /// <summary>
        /// 将 ROM 后端 JSON 响应反序列化为通用加密响应模型。
        /// </summary>
        /// <param name="responseText">服务端返回的 JSON 文本。</param>
        /// <returns>通用加密响应模型。</returns>
        private static RomApiEncryptedResponse DeserializeEncryptedResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new InvalidOperationException("ROM 接口响应为空。");
            }

            var responseBytes = Encoding.UTF8.GetBytes(responseText);
            using (var responseStream = new MemoryStream(responseBytes))
            {
                return ResponseSerializer.ReadObject(responseStream) as RomApiEncryptedResponse;
            }
        }
    }
}
