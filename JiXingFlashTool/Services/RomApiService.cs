using JiXingFlashTool.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JiXingFlashTool.Services
{
    /// <summary>
    /// ROM 后端业务接口服务，封装所有刷机工具接口调用与 data 解密流程。
    /// </summary>
    public sealed class RomApiService : IRomApiService
    {
        private static readonly Lazy<RomApiService> LazyInstance = new Lazy<RomApiService>(() => new RomApiService(RomApiClient.Instance));
        private readonly IRomApiClient _romApiClient;

        /// <summary>
        /// 使用默认 ROM 接口客户端初始化业务服务。
        /// </summary>
        public RomApiService()
            : this(RomApiClient.Instance)
        {
        }

        /// <summary>
        /// 使用指定 ROM 接口客户端初始化业务服务，便于测试或替换调用实现。
        /// </summary>
        /// <param name="romApiClient">ROM 接口客户端。</param>
        public RomApiService(IRomApiClient romApiClient)
        {
            _romApiClient = romApiClient ?? throw new ArgumentNullException(nameof(romApiClient));
        }

        /// <summary>
        /// 全局 ROM 后端业务接口服务实例。
        /// </summary>
        public static RomApiService Instance => LazyInstance.Value;

        /// <summary>
        /// 检查设备授权状态。
        /// </summary>
        /// <param name="request">设备授权检查请求对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public Task<string> CheckDeviceAuthorizationAsync(object request, CancellationToken cancellationToken)
        {
            return PostForDecryptedDataAsync(RomApiEndpointPaths.CheckDeviceAuthorization, request, cancellationToken);
        }

        /// <summary>
        /// 批量提交设备授权检查结果。
        /// </summary>
        /// <param name="request">批量授权结果请求对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public Task<string> SubmitDeviceAuthorizationResultsAsync(object request, CancellationToken cancellationToken)
        {
            return PostForDecryptedDataAsync(RomApiEndpointPaths.SubmitDeviceAuthorizationResults, request, cancellationToken);
        }

        /// <summary>
        /// 根据设备代号获取官方系统资源。
        /// </summary>
        /// <param name="deviceCode">设备代号。</param>
        /// <param name="extraQuery">附加查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public Task<string> GetOfficialSystemResourcesByCodeAsync(string deviceCode, IReadOnlyDictionary<string, string> extraQuery, CancellationToken cancellationToken)
        {
            return GetByDeviceCodeAsync(RomApiEndpointPaths.OfficialSystemResourcesByCode, deviceCode, extraQuery, cancellationToken);
        }

        /// <summary>
        /// 获取公开 ROM 列表。
        /// </summary>
        /// <param name="query">查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public Task<string> GetPublicRomListAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken)
        {
            return GetForDecryptedDataAsync(RomApiEndpointPaths.PublicRomList, query, cancellationToken);
        }

        /// <summary>
        /// 获取 ROM 提取密码。
        /// </summary>
        /// <param name="romId">ROM 资源标识。</param>
        /// <param name="extraQuery">附加查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public Task<string> GetRomExtractPasswordAsync(string romId, IReadOnlyDictionary<string, string> extraQuery, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(romId))
            {
                throw new ArgumentException("ROM 资源标识不能为空。", nameof(romId));
            }

            var query = MergeQuery(extraQuery, "romId", romId);
            return GetForDecryptedDataAsync(RomApiEndpointPaths.RomExtractPassword, query, cancellationToken);
        }

        /// <summary>
        /// 根据设备代号获取 ROM 资源。
        /// </summary>
        /// <param name="deviceCode">设备代号。</param>
        /// <param name="extraQuery">附加查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public Task<string> GetRomResourcesByCodeAsync(string deviceCode, IReadOnlyDictionary<string, string> extraQuery, CancellationToken cancellationToken)
        {
            return GetByDeviceCodeAsync(RomApiEndpointPaths.RomResourcesByCode, deviceCode, extraQuery, cancellationToken);
        }

        /// <summary>
        /// 根据设备代号获取 TWRP 资源。
        /// </summary>
        /// <param name="deviceCode">设备代号。</param>
        /// <param name="extraQuery">附加查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public Task<string> GetTwrpResourcesByCodeAsync(string deviceCode, IReadOnlyDictionary<string, string> extraQuery, CancellationToken cancellationToken)
        {
            return GetByDeviceCodeAsync(RomApiEndpointPaths.TwrpResourcesByCode, deviceCode, extraQuery, cancellationToken);
        }

        /// <summary>
        /// 获取客户端高级配置。
        /// </summary>
        /// <param name="query">查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public Task<string> GetClientAdvancedConfigAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken)
        {
            return GetForDecryptedDataAsync(RomApiEndpointPaths.ClientAdvancedConfig, query, cancellationToken);
        }

        /// <summary>
        /// 获取客户端公开配置。
        /// </summary>
        /// <param name="query">查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public Task<string> GetClientPublicConfigAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken)
        {
            return GetForDecryptedDataAsync(RomApiEndpointPaths.ClientPublicConfig, query, cancellationToken);
        }

        /// <summary>
        /// 执行客户端登录。
        /// </summary>
        /// <param name="request">客户端登录请求对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        public Task<string> LoginClientAsync(object request, CancellationToken cancellationToken)
        {
            return PostForDecryptedDataAsync(RomApiEndpointPaths.ClientLogin, request, cancellationToken);
        }

        /// <summary>
        /// 按设备代号发起 GET 请求。
        /// </summary>
        /// <param name="relativePath">接口相对路径。</param>
        /// <param name="deviceCode">设备代号。</param>
        /// <param name="extraQuery">附加查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        private Task<string> GetByDeviceCodeAsync(string relativePath, string deviceCode, IReadOnlyDictionary<string, string> extraQuery, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(deviceCode))
            {
                throw new ArgumentException("设备代号不能为空。", nameof(deviceCode));
            }

            var query = MergeQuery(extraQuery, "code", deviceCode);
            return GetForDecryptedDataAsync(relativePath, query, cancellationToken);
        }

        /// <summary>
        /// 发起 GET 请求并返回解密后的 data 明文。
        /// </summary>
        /// <param name="relativePath">接口相对路径。</param>
        /// <param name="query">查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        private Task<string> GetForDecryptedDataAsync(string relativePath, IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken)
        {
            return _romApiClient.SendForDecryptedDataAsync(AppendQuery(relativePath, query), HttpMethod.Get, null, cancellationToken);
        }

        /// <summary>
        /// 发起 POST JSON 请求并返回解密后的 data 明文。
        /// </summary>
        /// <param name="relativePath">接口相对路径。</param>
        /// <param name="request">请求对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        private Task<string> PostForDecryptedDataAsync(string relativePath, object request, CancellationToken cancellationToken)
        {
            var json = SerializeRequest(request ?? new Dictionary<string, string>());
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return _romApiClient.SendForDecryptedDataAsync(relativePath, HttpMethod.Post, content, cancellationToken);
        }

        /// <summary>
        /// 将请求对象序列化为 JSON 文本。
        /// </summary>
        /// <param name="request">请求对象。</param>
        /// <returns>JSON 请求文本。</returns>
        private static string SerializeRequest(object request)
        {
            var serializer = new DataContractJsonSerializer(request.GetType());
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, request);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>
        /// 合并查询参数，并确保指定业务参数优先生效。
        /// </summary>
        /// <param name="query">原始查询参数。</param>
        /// <param name="key">需要写入的参数名。</param>
        /// <param name="value">需要写入的参数值。</param>
        /// <returns>合并后的查询参数。</returns>
        private static IReadOnlyDictionary<string, string> MergeQuery(IReadOnlyDictionary<string, string> query, string key, string value)
        {
            var mergedQuery = new Dictionary<string, string>();
            if (query != null)
            {
                foreach (var queryItem in query)
                {
                    mergedQuery[queryItem.Key] = queryItem.Value;
                }
            }

            mergedQuery[key] = value;
            return mergedQuery;
        }

        /// <summary>
        /// 将查询参数追加到相对路径。
        /// </summary>
        /// <param name="relativePath">接口相对路径。</param>
        /// <param name="query">查询参数。</param>
        /// <returns>带查询字符串的接口相对路径。</returns>
        private static string AppendQuery(string relativePath, IReadOnlyDictionary<string, string> query)
        {
            if (query == null || query.Count == 0)
            {
                return relativePath;
            }

            var queryText = string.Join("&", query
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
                .Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)));

            return string.IsNullOrWhiteSpace(queryText)
                ? relativePath
                : relativePath + (relativePath.Contains("?") ? "&" : "?") + queryText;
        }
    }
}
