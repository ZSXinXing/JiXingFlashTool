using JXHeimdall.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXHeimdall.Services
{
    /// <summary>
    /// 负责解压所选 Odin 固件包、生成 Heimdall 命令并返回终端执行结果。
    /// </summary>
    public sealed class HeimdallFlashService
    {
        private static readonly string[] PartitionOrder =
        {
            "SBOOT",
            "CM",
            "PARAM",
            "XBL",
            "ABOOT",
            "RPM",
            "TZ",
            "HYP",
            "DEVCFG",
            "PMIC",
            "LKSECAPP",
            "KEYMASTER",
            "CMNLIB",
            "CMNLIB64",
            "APDP",
            "MSADP",
            "SEC",
            "APNHLOS",
            "BOOT",
            "RECOVERY",
            "SYSTEM",
            "VENDOR",
            "PERSIST",
            "MODEM",
            "DSP",
            "CACHE",
            "HIDDEN",
            "USERDATA"
        };

        private static readonly HeimdallFirmwareSlot[] PackageOrder =
        {
            HeimdallFirmwareSlot.BL,
            HeimdallFirmwareSlot.AP,
            HeimdallFirmwareSlot.CP,
            HeimdallFirmwareSlot.CSC,
            HeimdallFirmwareSlot.USERDATA,
            HeimdallFirmwareSlot.TWRP
        };

        private readonly HeimdallProcessService _processService;
        private readonly HeimdallFirmwarePackageService _firmwarePackageService;

        /// <summary>
        /// 初始化 Heimdall 固件刷入服务。
        /// </summary>
        /// <param name="processService">Heimdall 命令执行服务。</param>
        /// <param name="firmwarePackageService">固件包解压服务。</param>
        public HeimdallFlashService(
            HeimdallProcessService processService,
            HeimdallFirmwarePackageService firmwarePackageService)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _firmwarePackageService = firmwarePackageService ?? throw new ArgumentNullException(nameof(firmwarePackageService));
        }

        /// <summary>
        /// 解压并刷入所选 AP、BL、CP、CSC 固件包。
        /// </summary>
        /// <param name="request">刷入请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>Heimdall 执行结果。</returns>
        public Task<HeimdallFlashResult> FlashFirmwareAsync(
            HeimdallFlashRequest request,
            CancellationToken cancellationToken)
        {
            return FlashInternalAsync(request, cancellationToken);
        }

        /// <summary>
        /// 解压并刷入 TWRP recovery 镜像，刷入后保持 Download 模式。
        /// </summary>
        /// <param name="request">刷入请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>Heimdall 执行结果。</returns>
        public Task<HeimdallFlashResult> FlashTwrpAndBootRecoveryAsync(
            HeimdallFlashRequest request,
            CancellationToken cancellationToken)
        {
            return FlashInternalAsync(request, cancellationToken);
        }

        /// <summary>
        /// 执行固件解压、固定分区映射和单条 Heimdall flash 命令。
        /// </summary>
        /// <param name="request">刷入请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>Heimdall 执行结果。</returns>
        private async Task<HeimdallFlashResult> FlashInternalAsync(
            HeimdallFlashRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_processService.IsAvailable)
            {
                return new HeimdallFlashResult
                {
                    IsSuccess = false,
                    Message = "未找到 Resources\\Library\\Heimdall\\heimdall.exe。"
                };
            }

            var packages = new List<HeimdallFirmwarePackageModel>();
            var keepPackageWorkingDirectories = false;
            try
            {
                request.Log?.Invoke("正在解压所选 Odin 固件包。");
                foreach (var slot in PackageOrder)
                {
                    if (request.FirmwareFiles.TryGetValue(slot, out var filePath))
                    {
                        packages.Add(_firmwarePackageService.ParsePackage(slot, filePath));
                    }
                }

                ApplyRecoveryOverride(packages, request);
                LogPackageSummary(packages);
                var flashMap = BuildFlashMap(packages);
                LogFlashMap(flashMap);
                if (flashMap.Count == 0)
                {
                    return new HeimdallFlashResult
                    {
                        IsSuccess = false,
                        Message = "所选固件包中没有找到可由 Heimdall 刷入的镜像。"
                    };
                }

                var largeSystemImageError = ValidateLargeSystemImage(flashMap);
                if (!string.IsNullOrWhiteSpace(largeSystemImageError))
                {
                    request.Log?.Invoke(largeSystemImageError);
                    HeimdallDebugLogService.Write("Flash", largeSystemImageError);
                    return new HeimdallFlashResult
                    {
                        IsSuccess = false,
                        Message = largeSystemImageError
                    };
                }

                var pitFilePath = FindPitFilePath(packages);
                var shouldRepartition = false;
                var usbSelector = request.Device?.UsbSelector ?? string.Empty;
                if (string.IsNullOrWhiteSpace(usbSelector))
                {
                    return new HeimdallFlashResult
                    {
                        IsSuccess = false,
                        Message = "缺少 Heimdall USB 设备选择器，无法执行多设备并发刷入。"
                    };
                }

                HeimdallDebugLogService.Write(
                    "Flash",
                    "repartition Enabled=" + shouldRepartition + " Pit=" + pitFilePath + " UsbSelector=" + usbSelector);
                var skipSizeCheck = await ShouldSkipSystemSizeCheckAsync(
                    flashMap,
                    pitFilePath,
                    cancellationToken).ConfigureAwait(false);
                var arguments = BuildFlashArguments(flashMap, pitFilePath, shouldRepartition, skipSizeCheck, usbSelector);
                var result = await _processService
                    .ExecuteAsync(arguments, request.Log, cancellationToken)
                    .ConfigureAwait(false);

                keepPackageWorkingDirectories = !result.IsSuccess;
                return new HeimdallFlashResult
                {
                    IsSuccess = result.IsSuccess,
                    Message = result.IsSuccess
                        ? "Heimdall 固件刷入完成。"
                        : BuildFailureMessage(result)
                };
            }
            catch (Exception ex)
            {
                keepPackageWorkingDirectories = packages.Count > 0;
                HeimdallDebugLogService.Write("Flash", "failed " + ex);
                return new HeimdallFlashResult
                {
                    IsSuccess = false,
                    Message = "Heimdall 刷入失败：" + ex.Message
                };
            }
            finally
            {
                if (keepPackageWorkingDirectories)
                {
                    LogKeptPackageWorkingDirectories(packages);
                }
                else
                {
                    HeimdallFirmwarePackageService.CleanupPackageWorkingDirectories(packages);
                }
            }
        }

        /// <summary>
        /// 将用户选择的 TWRP 镜像替换为本次命令使用的 RECOVERY 文件。
        /// </summary>
        /// <param name="packages">已解压的固件包。</param>
        /// <param name="request">刷入请求。</param>
        private void ApplyRecoveryOverride(
            IEnumerable<HeimdallFirmwarePackageModel> packages,
            HeimdallFlashRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RecoveryOverrideFilePath))
            {
                return;
            }

            var apPackage = packages.FirstOrDefault(item => item.Slot == HeimdallFirmwareSlot.AP)
                ?? packages.FirstOrDefault();
            if (apPackage == null)
            {
                return;
            }

            request.Log?.Invoke("正在使用所选 TWRP 替换 recovery 镜像。");
            _firmwarePackageService.ApplyRecoveryOverride(apPackage, request.RecoveryOverrideFilePath);
        }

        /// <summary>
        /// 根据解压文件记录构建固定 Heimdall 分区映射。
        /// </summary>
        /// <param name="packages">已解压的固件包。</param>
        /// <returns>分区名与镜像绝对路径映射。</returns>
        private static IReadOnlyDictionary<string, string> BuildFlashMap(
            IEnumerable<HeimdallFirmwarePackageModel> packages)
        {
            var packageList = packages.ToList();
            var shouldFlashApUserdata = ShouldFlashApUserdata(packageList);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in packageList)
            {
                foreach (var file in package.Files)
                {
                    // 普通 CSC 是清数据官方刷机流程，需要跟 Odin3 一样刷入 AP 内 USERDATA；HOME_CSC 继续保留数据。
                    if (string.Equals(file.SuggestedPartitionName, "USERDATA", StringComparison.OrdinalIgnoreCase) &&
                        package.Slot != HeimdallFirmwareSlot.USERDATA &&
                        !(package.Slot == HeimdallFirmwareSlot.AP && shouldFlashApUserdata))
                    {
                        HeimdallDebugLogService.Write(
                            "FlashMap",
                            "skip-userdata Slot=" + package.Slot + " Path=" + file.ExtractedFilePath);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(file.SuggestedPartitionName))
                    {
                        result[file.SuggestedPartitionName] = file.ExtractedFilePath;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 判断是否应刷入 AP 包内的 USERDATA 镜像，普通 CSC 对齐 Odin3 清数据刷机流程，HOME_CSC 保留数据。
        /// </summary>
        /// <param name="packages">已解压的固件包。</param>
        /// <returns>需要刷入 AP USERDATA 时返回 true。</returns>
        private static bool ShouldFlashApUserdata(IEnumerable<HeimdallFirmwarePackageModel> packages)
        {
            var cscPackage = packages.FirstOrDefault(item => item.Slot == HeimdallFirmwareSlot.CSC);
            if (cscPackage == null)
            {
                return false;
            }

            var cscFileName = Path.GetFileName(cscPackage.SourceFilePath) ?? string.Empty;
            return !cscFileName.StartsWith("HOME_CSC", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 记录固件包解压结果，用于对比应用内命令和手工 CMD 命令的输入文件差异。
        /// </summary>
        /// <param name="packages">已解压的固件包集合。</param>
        private static void LogPackageSummary(IEnumerable<HeimdallFirmwarePackageModel> packages)
        {
            foreach (var package in packages)
            {
                HeimdallDebugLogService.Write(
                    "Package",
                    "summary Slot=" + package.Slot +
                    " Source=" + package.SourceFilePath +
                    " WorkDir=" + package.WorkingDirectory +
                    " FileCount=" + package.Files.Count +
                    " Pit=" + package.PitFilePath);

                foreach (var file in package.Files)
                {
                    HeimdallDebugLogService.Write(
                        "Package",
                        "file Slot=" + package.Slot +
                        " Partition=" + file.SuggestedPartitionName +
                        " Entry=" + file.EntryName +
                        " Size=" + file.Size +
                        " Path=" + file.ExtractedFilePath);
                }
            }
        }

        /// <summary>
        /// 记录最终参与刷入的分区映射，用于定位固定分区名或文件来源错误。
        /// </summary>
        /// <param name="flashMap">分区名与镜像路径映射。</param>
        private static void LogFlashMap(IReadOnlyDictionary<string, string> flashMap)
        {
            foreach (var partition in PartitionOrder)
            {
                if (!flashMap.TryGetValue(partition, out var filePath))
                {
                    continue;
                }

                var fileSize = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
                HeimdallDebugLogService.Write(
                    "FlashMap",
                    "Partition=" + partition + " Size=" + fileSize + " Path=" + filePath);
            }
        }

        /// <summary>
        /// 刷机失败时保留解压目录，方便直接复制日志中的 heimdall 命令到 CMD 复现。
        /// </summary>
        /// <param name="packages">本次刷机解压出的固件包集合。</param>
        private static void LogKeptPackageWorkingDirectories(IEnumerable<HeimdallFirmwarePackageModel> packages)
        {
            foreach (var package in packages)
            {
                if (string.IsNullOrWhiteSpace(package.WorkingDirectory))
                {
                    continue;
                }

                HeimdallDebugLogService.Write(
                    "Package",
                    "keep-workdir Slot=" + package.Slot + " Path=" + package.WorkingDirectory);
            }
        }

        /// <summary>
        /// 检查大 SYSTEM 镜像是否会触发 Heimdall 1.4.2 旧构建的 32 位长度异常。
        /// </summary>
        /// <param name="flashMap">分区名与镜像路径映射。</param>
        /// <returns>存在风险时返回错误说明，否则返回空字符串。</returns>
        private string ValidateLargeSystemImage(IReadOnlyDictionary<string, string> flashMap)
        {
            if (!_processService.HasKnownLargeSystemImageIssue ||
                !flashMap.TryGetValue("SYSTEM", out var systemImagePath) ||
                string.IsNullOrWhiteSpace(systemImagePath) ||
                !File.Exists(systemImagePath))
            {
                return string.Empty;
            }

            var systemImageSize = new FileInfo(systemImagePath).Length;
            if (systemImageSize <= int.MaxValue)
            {
                return string.Empty;
            }

            return "当前 Heimdall 为已知大文件异常旧构建，无法稳定刷入超过 2GB 的 SYSTEM 镜像。请确认 Resources\\Library\\Heimdall\\heimdall.exe 已替换为 2.2.2。SYSTEM 大小：" + systemImageSize + " 字节。";
        }

        /// <summary>
        /// 按固定分区顺序生成单条 heimdall flash 命令。
        /// </summary>
        /// <param name="flashMap">分区名与镜像路径映射。</param>
        /// <returns>Heimdall 命令参数。</returns>
        private static string BuildFlashArguments(
            IReadOnlyDictionary<string, string> flashMap,
            string pitFilePath,
            bool shouldRepartition,
            bool skipSizeCheck,
            string usbSelector)
        {
            var builder = new StringBuilder("flash");
            if (!string.IsNullOrWhiteSpace(usbSelector))
            {
                builder.Append(" --usb-selector ");
                builder.Append(HeimdallProcessService.Quote(usbSelector));
            }

            if (shouldRepartition && !string.IsNullOrWhiteSpace(pitFilePath))
            {
                builder.Append(" --repartition --pit ");
                builder.Append(HeimdallProcessService.Quote(pitFilePath));
            }

            foreach (var partition in PartitionOrder)
            {
                if (!flashMap.TryGetValue(partition, out var filePath))
                {
                    continue;
                }

                builder.Append(" --");
                builder.Append(partition);
                builder.Append(' ');
                builder.Append(HeimdallProcessService.Quote(filePath));
            }

            if (skipSizeCheck)
            {
                builder.Append(" --skip-size-check");
            }

            return builder.ToString();
        }

        /// <summary>
        /// 当本地 PIT 明确确认 SYSTEM 分区容量足够时，跳过 Heimdall 对 UFS 设备的错误大小校验。
        /// </summary>
        /// <param name="flashMap">分区名与镜像路径映射。</param>
        /// <param name="packages">已解压的固件包集合。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>需要追加 --skip-size-check 时返回 true。</returns>
        private async Task<bool> ShouldSkipSystemSizeCheckAsync(
            IReadOnlyDictionary<string, string> flashMap,
            string pitFilePath,
            CancellationToken cancellationToken)
        {
            if (!flashMap.TryGetValue("SYSTEM", out var systemImagePath) ||
                string.IsNullOrWhiteSpace(systemImagePath) ||
                !File.Exists(systemImagePath))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(pitFilePath))
            {
                return false;
            }

            var result = await _processService
                .ExecuteAsync("print-pit --file " + HeimdallProcessService.Quote(pitFilePath), null, cancellationToken)
                .ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                HeimdallDebugLogService.Write("Flash", "system-size-check-pit-read-failed Pit=" + pitFilePath);
                return false;
            }

            var systemBlockCount = ReadPitPartitionBlockCount(result.StandardOutput, "SYSTEM");
            if (systemBlockCount <= 0)
            {
                HeimdallDebugLogService.Write("Flash", "system-size-check-missing-system Pit=" + pitFilePath);
                return false;
            }

            var systemImageSize = new FileInfo(systemImagePath).Length;
            var systemPartitionSize = systemBlockCount * 4096L;
            var shouldSkip = systemImageSize <= systemPartitionSize;
            HeimdallDebugLogService.Write(
                "Flash",
                "system-size-check ImageSize=" + systemImageSize +
                " PitBlockCount=" + systemBlockCount +
                " EstimatedPartitionSize=" + systemPartitionSize +
                " SkipSizeCheck=" + shouldSkip);

            return shouldSkip;
        }

        /// <summary>
        /// 从 Heimdall print-pit 输出中读取指定分区的 Block Count。
        /// </summary>
        /// <param name="pitText">print-pit 输出文本。</param>
        /// <param name="partitionName">分区名。</param>
        /// <returns>分区 Block Count，读取失败返回 0。</returns>
        private static long ReadPitPartitionBlockCount(string pitText, string partitionName)
        {
            if (string.IsNullOrWhiteSpace(pitText) || string.IsNullOrWhiteSpace(partitionName))
            {
                return 0;
            }

            var entries = pitText.Split(new[] { "--- Entry #" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                if (entry.IndexOf("Partition Name: " + partitionName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                foreach (var line in entry.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var text = line.Trim();
                    const string prefix = "Partition Block Count:";
                    if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return long.TryParse(text.Substring(prefix.Length).Trim(), out var blockCount)
                        ? blockCount
                        : 0;
                }
            }

            return 0;
        }

        /// <summary>
        /// 从固件包中选择本地 PIT 文件，优先使用 CSC 包内 PIT。
        /// </summary>
        /// <param name="packages">已解压的固件包集合。</param>
        /// <returns>可用 PIT 文件路径。</returns>
        private static string FindPitFilePath(IEnumerable<HeimdallFirmwarePackageModel> packages)
        {
            if (packages == null)
            {
                return string.Empty;
            }

            return packages
                .OrderBy(item => item.Slot == HeimdallFirmwareSlot.CSC ? 0 : 1)
                .Select(item => item.PitFilePath)
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item) && File.Exists(item)) ?? string.Empty;
        }

        /// <summary>
        /// 从固件包中选择用于重分区的 PIT 文件，优先使用 CSC 包内 PIT。
        /// </summary>
        /// <param name="packages">已解析的固件包集合。</param>
        /// <returns>可用 PIT 文件路径；没有 PIT 时返回空字符串。</returns>
        /// <summary>
        /// 从 Heimdall 标准错误或标准输出中提取用户可读的失败原因。
        /// </summary>
        /// <param name="result">Heimdall 进程结果。</param>
        /// <returns>失败提示。</returns>
        private static string BuildFailureMessage(HeimdallProcessResult result)
        {
            var detail = FirstMeaningfulLine(result.StandardError);
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = FirstMeaningfulLine(result.StandardOutput);
            }

            return string.IsNullOrWhiteSpace(detail)
                ? "Heimdall 刷入失败，退出码：" + result.ExitCode + "。"
                : "Heimdall 刷入失败：" + detail;
        }

        /// <summary>
        /// 获取终端输出中的第一条有效文本。
        /// </summary>
        /// <param name="text">终端输出。</param>
        /// <returns>第一条非空文本。</returns>
        private static string FirstMeaningfulLine(string text)
        {
            return (text ?? string.Empty)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? string.Empty;
        }
    }
}
