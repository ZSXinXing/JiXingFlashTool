using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JiXingFlashTool.Interface
{
    /// <summary>
    /// ROM 后端业务接口服务抽象，供页面、ViewModel 或其它服务复用接口能力。
    /// </summary>
    public interface IRomApiService
    {
        /// <summary>
        /// 检查设备授权状态。
        /// </summary>
        /// <param name="request">设备授权检查请求对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> CheckDeviceAuthorizationAsync(object request, CancellationToken cancellationToken);

        /// <summary>
        /// 批量提交设备授权检查结果。
        /// </summary>
        /// <param name="request">批量授权结果请求对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> SubmitDeviceAuthorizationResultsAsync(object request, CancellationToken cancellationToken);

        /// <summary>
        /// 根据设备代号获取官方系统资源。
        /// </summary>
        /// <param name="deviceCode">设备代号。</param>
        /// <param name="extraQuery">附加查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> GetOfficialSystemResourcesByCodeAsync(string deviceCode, IReadOnlyDictionary<string, string> extraQuery, CancellationToken cancellationToken);

        /// <summary>
        /// 获取公开 ROM 列表。
        /// </summary>
        /// <param name="query">查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> GetPublicRomListAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken);

        /// <summary>
        /// 获取 ROM 提取密码。
        /// </summary>
        /// <param name="romId">ROM 资源标识。</param>
        /// <param name="extraQuery">附加查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> GetRomExtractPasswordAsync(string romId, IReadOnlyDictionary<string, string> extraQuery, CancellationToken cancellationToken);

        /// <summary>
        /// 根据设备代号获取 ROM 资源。
        /// </summary>
        /// <param name="deviceCode">设备代号。</param>
        /// <param name="extraQuery">附加查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> GetRomResourcesByCodeAsync(string deviceCode, IReadOnlyDictionary<string, string> extraQuery, CancellationToken cancellationToken);

        /// <summary>
        /// 根据设备代号获取 TWRP 资源。
        /// </summary>
        /// <param name="deviceCode">设备代号。</param>
        /// <param name="extraQuery">附加查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> GetTwrpResourcesByCodeAsync(string deviceCode, IReadOnlyDictionary<string, string> extraQuery, CancellationToken cancellationToken);

        /// <summary>
        /// 获取客户端高级配置。
        /// </summary>
        /// <param name="query">查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> GetClientAdvancedConfigAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken);

        /// <summary>
        /// 获取客户端公开配置。
        /// </summary>
        /// <param name="query">查询参数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> GetClientPublicConfigAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken);

        /// <summary>
        /// 执行客户端登录。
        /// </summary>
        /// <param name="request">客户端登录请求对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>解密后的 data 明文。</returns>
        Task<string> LoginClientAsync(object request, CancellationToken cancellationToken);
    }
}
