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
    /// 负责组织 BL、AP、CP、CSC、USERDATA 与 TWRP 的 Heimdall 刷入流程。
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
        /// 按普通 Odin 固件流程刷入 BL、AP、CP、CSC、USERDATA。
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
            var packages = new List<HeimdallFirmwarePackageModel>();
            foreach (var firmwareFile in request.FirmwareFiles)
            {
                packages.Add(_firmwarePackageService.ParsePackage(firmwareFile.Key, firmwareFile.Value));
            }

            var packagePitFilePath = packages
                .Select(item => item.PitFilePath)
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
            var partitions = string.IsNullOrWhiteSpace(packagePitFilePath)
                ? await _pitService.ReadPartitionsAsync(log, cancellationToken).ConfigureAwait(false)
                : await _pitService.ReadPartitionsFromFileAsync(packagePitFilePath, log, cancellationToken).ConfigureAwait(false);

            var flashMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in packages)
            {
                var packageFlashMap = BuildPackageFlashMap(package, package.Slot, partitions, isTwrpMode);
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
                var failureMessage = BuildFlashFailureMessage(result);
                log?.Invoke(failureMessage);
                return new HeimdallFlashResult
                {
                    IsSuccess = false,
                    Message = failureMessage,
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

        /// <summary>
        /// 根据 Heimdall 进程输出构造可展示的失败原因，避免任务界面只显示泛化失败。
        /// </summary>
        /// <param name="result">Heimdall 进程执行结果。</param>
        /// <returns>包含退出码和关键输出的失败描述。</returns>
        private static string BuildFlashFailureMessage(HeimdallProcessResult result)
        {
            if (result == null)
            {
                return "Heimdall 刷入失败。";
            }

            var detail = FirstMeaningfulLine(result.StandardError);
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = FirstMeaningfulLine(result.StandardOutput);
            }

            return string.IsNullOrWhiteSpace(detail)
                ? "Heimdall 刷入失败，退出码：" + result.ExitCode + "。"
                : "Heimdall 刷入失败，退出码：" + result.ExitCode + "，原因：" + detail;
        }

        /// <summary>
        /// 从进程输出中提取第一条有意义的错误行。
        /// </summary>
        /// <param name="text">进程标准输出或错误输出。</param>
        /// <returns>第一条非空输出行。</returns>
        private static string FirstMeaningfulLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? string.Empty;
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

        /// <summary>
        /// 根据 Odin 固件槽位选择对应的 Heimdall 分区映射策略。
        /// </summary>
        /// <param name="package">已解析的固件包。</param>
        /// <param name="slot">Odin 固件槽位。</param>
        /// <param name="partitions">PIT 分区集合。</param>
        /// <param name="isTwrpMode">是否为 TWRP 刷入模式。</param>
        /// <returns>Heimdall 分区与文件路径映射。</returns>
        private IReadOnlyDictionary<string, string> BuildPackageFlashMap(
            HeimdallFirmwarePackageModel package,
            HeimdallFirmwareSlot slot,
            IReadOnlyList<HeimdallPitPartitionModel> partitions,
            bool isTwrpMode)
        {
            if (isTwrpMode || slot == HeimdallFirmwareSlot.TWRP)
            {
                return _firmwarePackageService.BuildTwrpFlashMap(package, partitions);
            }

            if (slot == HeimdallFirmwareSlot.CP)
            {
                return _firmwarePackageService.BuildCpFlashMap(package, partitions);
            }

            return _firmwarePackageService.BuildFlashMap(package, partitions);
        }
    }
}
