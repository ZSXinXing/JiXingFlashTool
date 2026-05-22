using JXHeimdall.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JXHeimdall.Services
{
    /// <summary>
    /// 负责读取并解析 Samsung 设备 PIT 分区表。
    /// </summary>
    public sealed class HeimdallPitService
    {
        private static readonly Regex EntryRegex = new Regex(@"--- Entry #(?<index>\d+) ---", RegexOptions.Compiled);
        private static IReadOnlyList<HeimdallPitPartitionModel> _lastSuccessfulPartitions;
        private readonly HeimdallProcessService _processService;

        /// <summary>
        /// 初始化 PIT 服务。
        /// </summary>
        /// <param name="processService">Heimdall 进程服务。</param>
        public HeimdallPitService(HeimdallProcessService processService)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        }

        /// <summary>
        /// 读取当前 Download 设备的 PIT 分区信息。
        /// </summary>
        /// <param name="log">任务详情日志回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>PIT 分区集合。</returns>
        public async Task<IReadOnlyList<HeimdallPitPartitionModel>> ReadPartitionsAsync(Action<string> log, CancellationToken cancellationToken)
        {
            var result = await _processService.ExecuteAsync("print-pit --no-reboot", log, cancellationToken).ConfigureAwait(false);
            LogProcessResult("print-pit-before-driver", result);

            if (!result.IsSuccess)
            {
                if (IsProtocolInitialisationFailure(result))
                {
                    if (_lastSuccessfulPartitions != null && _lastSuccessfulPartitions.Count > 0)
                    {
                        log?.Invoke("直接读取 PIT 失败，已复用本次运行中上一次成功读取的 PIT 分区表。");
                        HeimdallDebugLogService.Write("Pit", "UseCachedPartitionsAfterProtocolFailure Count=" + _lastSuccessfulPartitions.Count);
                        return _lastSuccessfulPartitions;
                    }

                    var fallbackPartitions = await TryReadPartitionsByDownloadedPitAsync(log, cancellationToken).ConfigureAwait(false);
                    if (fallbackPartitions != null)
                    {
                        return fallbackPartitions;
                    }

                    throw new InvalidOperationException("读取 PIT 失败：已尝试直接读取 PIT 和下载 PIT 文件，但 Heimdall 均在协议初始化阶段失败。请重新插拔设备或重新进入 Download 模式后重试；如果仍失败，可能是当前设备 Download 协议与 Heimdall 1.4.2 不兼容。");
                }

                throw new InvalidOperationException(BuildReadPitFailureMessage(result));
            }

            return CachePartitions(ParsePartitions(result.StandardOutput + Environment.NewLine + result.StandardError));
        }

        /// <summary>
        /// 从本地 PIT 文件解析分区表，用于固件包内已包含 PIT 文件的场景。
        /// </summary>
        /// <param name="pitFilePath">本地 PIT 文件路径。</param>
        /// <param name="log">任务详情日志回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>PIT 分区集合。</returns>
        public async Task<IReadOnlyList<HeimdallPitPartitionModel>> ReadPartitionsFromFileAsync(string pitFilePath, Action<string> log, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(pitFilePath) || !File.Exists(pitFilePath))
            {
                throw new FileNotFoundException("PIT 文件不存在，无法解析分区表。", pitFilePath);
            }

            log?.Invoke("正在从固件包内 PIT 文件解析分区表。");
            var result = await _processService.ExecuteAsync(
                "print-pit --file " + HeimdallProcessService.Quote(pitFilePath),
                log,
                cancellationToken).ConfigureAwait(false);
            LogProcessResult("print-package-pit", result);
            if (!result.IsSuccess)
            {
                var error = (result.StandardError ?? string.Empty).Trim();
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "解析固件包内 PIT 文件失败。" : "解析固件包内 PIT 文件失败：" + error);
            }

            return CachePartitions(ParsePartitions(result.StandardOutput + Environment.NewLine + result.StandardError));
        }

        /// <summary>
        /// 在直接 print-pit 协议初始化失败时，尝试先下载 PIT 文件再从本地文件解析。
        /// </summary>
        /// <param name="log">任务详情日志回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>成功解析时返回分区集合，失败时返回 null。</returns>
        private async Task<IReadOnlyList<HeimdallPitPartitionModel>> TryReadPartitionsByDownloadedPitAsync(Action<string> log, CancellationToken cancellationToken)
        {
            var workingDirectory = Path.Combine(Path.GetTempPath(), "JXHeimdall", "Pit", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingDirectory);
            var pitFilePath = Path.Combine(workingDirectory, "device.pit");

            log?.Invoke("直接读取 PIT 失败，正在尝试下载 PIT 文件后解析。");
            var downloadResult = await _processService.ExecuteAsync(
                "download-pit --output " + HeimdallProcessService.Quote(pitFilePath) + " --no-reboot",
                log,
                cancellationToken).ConfigureAwait(false);
            LogProcessResult("download-pit-fallback", downloadResult);
            if (!downloadResult.IsSuccess || !File.Exists(pitFilePath))
            {
                return null;
            }

            var printResult = await _processService.ExecuteAsync(
                "print-pit --file " + HeimdallProcessService.Quote(pitFilePath),
                log,
                cancellationToken).ConfigureAwait(false);
            LogProcessResult("print-downloaded-pit", printResult);
            return printResult.IsSuccess
                ? CachePartitions(ParsePartitions(printResult.StandardOutput + Environment.NewLine + printResult.StandardError))
                : null;
        }

        /// <summary>
        /// 从 Heimdall 输出内容解析 PIT 分区列表。
        /// </summary>
        /// <param name="pitOutput">print-pit 输出内容。</param>
        /// <returns>PIT 分区列表。</returns>
        public IReadOnlyList<HeimdallPitPartitionModel> ParsePartitions(string pitOutput)
        {
            var partitions = new List<HeimdallPitPartitionModel>();
            HeimdallPitPartitionModel current = null;
            foreach (var rawLine in (pitOutput ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var line = rawLine.Trim();
                var entryMatch = EntryRegex.Match(line);
                if (entryMatch.Success)
                {
                    current = new HeimdallPitPartitionModel
                    {
                        Index = int.Parse(entryMatch.Groups["index"].Value)
                    };
                    partitions.Add(current);
                    continue;
                }

                if (current == null)
                {
                    continue;
                }

                if (line.StartsWith("Partition Name:", StringComparison.OrdinalIgnoreCase))
                {
                    current.PartitionName = line.Substring("Partition Name:".Length).Trim();
                }
                else if (line.StartsWith("Filename:", StringComparison.OrdinalIgnoreCase))
                {
                    current.FileName = line.Substring("Filename:".Length).Trim();
                }
            }

            return partitions;
        }

        /// <summary>
        /// 根据 Heimdall 读取 PIT 的失败输出生成用户可理解的错误信息。
        /// </summary>
        /// <param name="result">Heimdall 进程执行结果。</param>
        /// <returns>适合任务详情展示的失败原因。</returns>
        private static string BuildReadPitFailureMessage(HeimdallProcessResult result)
        {
            if (IsDriverAccessFailure(result))
            {
                return "读取 PIT 失败：Heimdall 已检测到设备，但 Windows 当前驱动不支持 libusb 打开设备。软件已经尝试自动修复 WinUSB 驱动，请确认管理员权限弹窗已允许，然后重新插拔设备或重新进入 Download 模式后重试。";
            }

            var error = (result?.StandardError ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(error) ? "读取 PIT 失败。" : "读取 PIT 失败：" + error;
        }

        /// <summary>
        /// 判断 Heimdall 失败原因是否为 Windows USB 驱动不支持 libusb 访问。
        /// </summary>
        /// <param name="result">Heimdall 进程执行结果。</param>
        /// <returns>驱动访问失败时返回 true。</returns>
        private static bool IsDriverAccessFailure(HeimdallProcessResult result)
        {
            var output = (result?.StandardOutput ?? string.Empty) + Environment.NewLine + (result?.StandardError ?? string.Empty);
            return output.IndexOf("libusb error: -12", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   output.IndexOf("Failed to access device", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 判断 Heimdall 是否在协议初始化阶段失败，用于触发下载 PIT 文件的备用流程。
        /// </summary>
        /// <param name="result">Heimdall 进程执行结果。</param>
        /// <returns>协议初始化失败时返回 true。</returns>
        private static bool IsProtocolInitialisationFailure(HeimdallProcessResult result)
        {
            var output = (result?.StandardOutput ?? string.Empty) + Environment.NewLine + (result?.StandardError ?? string.Empty);
            return output.IndexOf("Protocol initialisation failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   output.IndexOf("Protocol initialization failed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 缓存最近一次成功读取的 PIT 分区表，避免同一 Download 会话中重复读取 PIT 导致协议初始化失败。
        /// </summary>
        /// <param name="partitions">本次成功解析的 PIT 分区集合。</param>
        /// <returns>原始分区集合。</returns>
        private static IReadOnlyList<HeimdallPitPartitionModel> CachePartitions(IReadOnlyList<HeimdallPitPartitionModel> partitions)
        {
            if (partitions != null && partitions.Count > 0)
            {
                _lastSuccessfulPartitions = partitions;
            }

            return partitions;
        }

        private static void LogProcessResult(string stage, HeimdallProcessResult result)
        {
            HeimdallDebugLogService.Write("Pit", stage + " ExitCode=" + result?.ExitCode);
            HeimdallDebugLogService.Write("Pit", stage + " StdOut=" + (result?.StandardOutput ?? string.Empty));
            HeimdallDebugLogService.Write("Pit", stage + " StdErr=" + (result?.StandardError ?? string.Empty));
        }
    }
}
