using JXHeimdall.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXHeimdall.Services
{
    /// <summary>
    /// 负责组织 BL、AP、CSC、USERDATA 与 TWRP 的 Heimdall 刷入流程。
    /// </summary>
    public sealed class HeimdallFlashService
    {
        private readonly HeimdallProcessService _processService;
        private readonly HeimdallPitService _pitService;
        private readonly HeimdallFirmwarePackageService _firmwarePackageService;

        /// <summary>
        /// 初始化 Heimdall 刷入服务。
        /// </summary>
        /// <param name="processService">Heimdall 进程服务。</param>
        /// <param name="pitService">PIT 服务。</param>
        /// <param name="firmwarePackageService">固件包服务。</param>
        public HeimdallFlashService(
            HeimdallProcessService processService,
            HeimdallPitService pitService,
            HeimdallFirmwarePackageService firmwarePackageService)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _pitService = pitService ?? throw new ArgumentNullException(nameof(pitService));
            _firmwarePackageService = firmwarePackageService ?? throw new ArgumentNullException(nameof(firmwarePackageService));
        }

        /// <summary>
        /// 按普通 Odin 固件流程刷入 BL、AP、CSC、USERDATA。
        /// </summary>
        /// <param name="request">刷机请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>刷机结果。</returns>
        public async Task<HeimdallFlashResult> FlashFirmwareAsync(HeimdallFlashRequest request, CancellationToken cancellationToken)
        {
            return await FlashInternalAsync(request, false, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 刷入 TWRP 并进入 Recovery 引导流程。
        /// </summary>
        /// <param name="request">刷机请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>刷机结果。</returns>
        public async Task<HeimdallFlashResult> FlashTwrpAndBootRecoveryAsync(HeimdallFlashRequest request, CancellationToken cancellationToken)
        {
            request.RebootToRecoveryAfterFlash = true;
            return await FlashInternalAsync(request, true, cancellationToken).ConfigureAwait(false);
        }

        private async Task<HeimdallFlashResult> FlashInternalAsync(HeimdallFlashRequest request, bool isTwrpMode, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var log = request.Log;
            var partitions = await _pitService.ReadPartitionsAsync(log, cancellationToken).ConfigureAwait(false);
            var flashMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var firmwareFile in request.FirmwareFiles)
            {
                var package = _firmwarePackageService.ParsePackage(firmwareFile.Key, firmwareFile.Value);
                var packageFlashMap = isTwrpMode || firmwareFile.Key == HeimdallFirmwareSlot.TWRP
                    ? _firmwarePackageService.BuildTwrpFlashMap(package, partitions)
                    : _firmwarePackageService.BuildFlashMap(package, partitions);
                foreach (var item in packageFlashMap)
                {
                    flashMap[item.Key] = item.Value;
                }
            }

            if (flashMap.Count == 0)
            {
                return new HeimdallFlashResult
                {
                    IsSuccess = false,
                    Message = "未找到与当前设备 PIT 匹配的可刷入分区。"
                };
            }

            var arguments = BuildFlashArguments(flashMap, isTwrpMode);
            var result = await _processService.ExecuteAsync(arguments, log, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return new HeimdallFlashResult
                {
                    IsSuccess = false,
                    Message = "Heimdall 刷入失败。",
                    LastProcessResult = result
                };
            }

            if (isTwrpMode)
            {
                log?.Invoke("TWRP 已刷入。已使用 --no-reboot 阻止普通重启，请立即进入 Recovery。");
                return new HeimdallFlashResult
                {
                    IsSuccess = true,
                    Message = "TWRP 已刷入，已进入 Recovery 引导流程。",
                    LastProcessResult = result
                };
            }

            return new HeimdallFlashResult
            {
                IsSuccess = true,
                Message = "固件刷入完成。",
                LastProcessResult = result
            };
        }

        private static string BuildFlashArguments(IReadOnlyDictionary<string, string> flashMap, bool noReboot)
        {
            var builder = new StringBuilder("flash --resume");
            foreach (var item in flashMap.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append(" --");
                builder.Append(item.Key);
                builder.Append(' ');
                builder.Append(HeimdallProcessService.Quote(item.Value));
            }

            if (noReboot)
            {
                builder.Append(" --no-reboot");
            }

            return builder.ToString();
        }
    }
}
