using JXHeimdall.Models;
using System.Threading;
using System.Threading.Tasks;
using TaskCore.Abstractions;

namespace JiXingFlashTool.Interface
{
    /// <summary>
    /// Heimdall 刷机能力接口，供 TaskCore 任务统一执行 Download 模式刷机流程。
    /// </summary>
    public interface IHeimdallCapability : IDeviceCapability
    {
        /// <summary>
        /// 执行普通 Odin 固件刷入，完成后由 Heimdall 默认流程重启设备。
        /// </summary>
        /// <param name="request">Heimdall 刷机请求。</param>
        /// <param name="cancellationToken">任务取消令牌。</param>
        /// <returns>刷机结果。</returns>
        Task<HeimdallFlashResult> FlashFirmwareAndRebootAsync(HeimdallFlashRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// 执行 TWRP 刷入并进入 Recovery 的刷机流程。
        /// </summary>
        /// <param name="request">Heimdall 刷机请求。</param>
        /// <param name="cancellationToken">任务取消令牌。</param>
        /// <returns>刷机结果。</returns>
        Task<HeimdallFlashResult> FlashTwrpAndRebootRecoveryAsync(HeimdallFlashRequest request, CancellationToken cancellationToken);
    }
}
