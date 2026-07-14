using JiXingFlashTool.Interface;
using JXHeimdall.Models;
using JXHeimdall.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JiXingFlashTool.Capability
{
    /// <summary>
    /// Heimdall 能力实现，负责把 JXHeimdall 服务封装成 TaskCore 可调用的设备能力。
    /// </summary>
    public sealed class HeimdallCapability : IHeimdallCapability
    {
        private readonly HeimdallFlashService _flashService;

        /// <summary>
        /// 初始化 Heimdall 刷机能力。
        /// </summary>
        public HeimdallCapability()
        {
            var processService = new HeimdallProcessService();
            var firmwarePackageService = new HeimdallFirmwarePackageService();
            _flashService = new HeimdallFlashService(processService, firmwarePackageService);
        }

        /// <summary>
        /// 执行普通 Odin 固件刷入，完成后由 Heimdall 默认流程重启设备。
        /// </summary>
        /// <param name="request">Heimdall 刷机请求。</param>
        /// <param name="cancellationToken">任务取消令牌。</param>
        /// <returns>刷机结果。</returns>
        public Task<HeimdallFlashResult> FlashFirmwareAndRebootAsync(HeimdallFlashRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return _flashService.FlashFirmwareAsync(request, cancellationToken);
        }

        /// <summary>
        /// 执行 TWRP 刷入并进入 Recovery 的刷机流程。
        /// </summary>
        /// <param name="request">Heimdall 刷机请求。</param>
        /// <param name="cancellationToken">任务取消令牌。</param>
        /// <returns>刷机结果。</returns>
        public Task<HeimdallFlashResult> FlashTwrpAndRebootRecoveryAsync(HeimdallFlashRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return _flashService.FlashTwrpAndBootRecoveryAsync(request, cancellationToken);
        }
    }
}
