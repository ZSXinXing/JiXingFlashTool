using JXHeimdall.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JXHeimdall.Services
{
    /// <summary>
    /// Windows 串口 Odin 固件刷入服务，负责解析 Odin 包并通过设备暴露的 COM 口执行刷写。
    /// </summary>
    public sealed class OdinSerialFlashService
    {
        private readonly HeimdallFirmwarePackageService _firmwarePackageService;
        private readonly OdinSerialPortService _serialPortService;
        private readonly OdinPitParserService _pitParserService;

        /// <summary>
        /// 初始化 Windows 串口 Odin 固件刷入服务。
        /// </summary>
        /// <param name="firmwarePackageService">Odin 固件包解析服务。</param>
        /// <param name="serialPortService">Odin 串口定位服务。</param>
        /// <param name="pitParserService">PIT 二进制解析服务。</param>
        public OdinSerialFlashService(
            HeimdallFirmwarePackageService firmwarePackageService,
            OdinSerialPortService serialPortService,
            OdinPitParserService pitParserService)
        {
            _firmwarePackageService = firmwarePackageService ?? throw new ArgumentNullException(nameof(firmwarePackageService));
            _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
            _pitParserService = pitParserService ?? throw new ArgumentNullException(nameof(pitParserService));
        }

        /// <summary>
        /// 通过 Windows 串口 Odin 协议执行普通固件刷入，完成后请求设备重启。
        /// </summary>
        /// <param name="request">刷入请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>刷入结果。</returns>
        public Task<HeimdallFlashResult> FlashFirmwareAsync(HeimdallFlashRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return Task.Run(() => FlashFirmwareInternal(request, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// 通过 Windows 串口 Odin 协议刷入 TWRP，并在刷入完成后尝试让设备直接进入 Recovery。
        /// </summary>
        /// <param name="request">刷入请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>刷入结果。</returns>
        public Task<HeimdallFlashResult> FlashTwrpAndRebootRecoveryAsync(HeimdallFlashRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.RebootToRecoveryAfterFlash = true;
            return Task.Run(() => FlashFirmwareInternal(request, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// 执行同步 Odin 串口刷入流程，外层通过 Task.Run 避免阻塞 UI 线程。
        /// </summary>
        /// <param name="request">刷入请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>刷入结果。</returns>
        private HeimdallFlashResult FlashFirmwareInternal(HeimdallFlashRequest request, CancellationToken cancellationToken)
        {
            List<HeimdallFirmwarePackageModel> packages = null;
            var shouldCleanupPackages = false;
            try
            {
                var log = request.Log;
                log?.Invoke("正在解析 Odin 固件包。");
                packages = ParsePackages(request);
                ApplyRecoveryOverride(packages, request);
                var portName = _serialPortService.ResolvePortName(request.Device);
                log?.Invoke("已找到 Odin 串口 " + portName + "，正在与设备握手。");
                HeimdallDebugLogService.Write("OdinSerial", "Port=" + portName);

                using (var protocol = new OdinSerialProtocolService(portName))
                {
                    protocol.OpenAndHandshake();
                    log?.Invoke("Odin 握手成功。");
                    protocol.BeginSession(cancellationToken);
                    log?.Invoke("Odin 会话已建立。");

                    log?.Invoke("正在读取设备 PIT 分区表。");
                    var pitBytes = protocol.DumpPit(cancellationToken);
                    var pitEntries = _pitParserService.Parse(pitBytes);
                    log?.Invoke("正在匹配固件分区。");
                    var flashItems = BuildFlashItems(packages, pitEntries);
                    LogFlashItems(flashItems);
                    if (flashItems.Count == 0)
                    {
                        return new HeimdallFlashResult
                        {
                            IsSuccess = false,
                            Message = "未找到与当前设备 PIT 匹配的可刷入分区。"
                        };
                    }

                    var totalBytes = flashItems.Sum(item => new FileInfo(item.FilePath).Length);
                    log?.Invoke("待刷入 " + flashItems.Count + " 个分区，共 " + FormatBytes(totalBytes) + "。");
                    protocol.SetTotalBytes(totalBytes, cancellationToken);
                    foreach (var item in flashItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        log?.Invoke("正在刷入 " + item.Entry.PartitionName + "：" + Path.GetFileName(item.FilePath));
                        HeimdallDebugLogService.Write("OdinSerial", "Flash " + item.Entry.PartitionName + " File=" + item.FilePath);
                        using (var stream = File.OpenRead(item.FilePath))
                        {
                            protocol.FlashPartition(
                                stream,
                                item.Entry,
                                (sent, total, stage) => log?.Invoke(stage + " " + FormatProgress(sent, total)),
                                cancellationToken);
                        }
                    }

                    protocol.EndSession(cancellationToken);
                    if (request.RebootToRecoveryAfterFlash)
                    {
                        TryRebootToRecovery(protocol, log, cancellationToken);
                    }
                    else
                    {
                        protocol.Reboot(cancellationToken);
                    }
                }

                shouldCleanupPackages = true;
                return new HeimdallFlashResult
                {
                    IsSuccess = true,
                    Message = request.RebootToRecoveryAfterFlash
                        ? "TWRP 已刷入，已执行自动重启进入 Recovery 流程。"
                        : "Odin 固件刷入完成。"
                };
            }
            catch (Exception ex)
            {
                HeimdallDebugLogService.Write("OdinSerial", "Failed " + ex);
                return new HeimdallFlashResult
                {
                    IsSuccess = false,
                    Message = "Odin 串口刷入失败：" + BuildUserFailureMessage(ex)
                };
            }
            finally
            {
                if (shouldCleanupPackages && packages != null)
                {
                    HeimdallFirmwarePackageService.CleanupPackageWorkingDirectories(packages);
                }
            }
        }

        /// <summary>
        /// 将底层串口异常转换为适合任务详情展示的刷入失败原因。
        /// </summary>
        /// <param name="exception">刷入过程中捕获的异常。</param>
        /// <returns>用户可读的失败原因。</returns>
        private static string BuildUserFailureMessage(Exception exception)
        {
            var message = exception.Message ?? string.Empty;
            if (message.Contains("设备没有发挥作用") || message.Contains("semaphore timeout"))
            {
                return "Windows 已识别 Download 设备，但当前无法打开 Odin 串口。请拔插 USB 并让手机重新进入 Download 模式后重试。";
            }

            return message;
        }

        /// <summary>
        /// 尝试发送 Odin Recovery 重启命令，失败时记录日志并保留刷入成功结果。
        /// </summary>
        /// <param name="protocol">当前 Odin 串口协议服务。</param>
        /// <param name="log">任务日志回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>设备接受 Recovery 重启命令时返回 true。</returns>
        private static bool TryRebootToRecovery(OdinSerialProtocolService protocol, Action<string> log, CancellationToken cancellationToken)
        {
            try
            {
                log?.Invoke("TWRP 已刷入，正在尝试自动进入 Recovery。");
                protocol.RebootToRecovery(cancellationToken);
                log?.Invoke("已发送自动进入 Recovery 命令。");
                return true;
            }
            catch (Exception ex)
            {
                HeimdallDebugLogService.Write("OdinSerial", "RebootToRecovery failed " + ex);
                log?.Invoke("设备未接受专用 Recovery 命令，正在执行自动重启。");
                protocol.Reboot(cancellationToken);
                log?.Invoke("已执行自动重启，等待设备进入 Recovery。");
                return false;
            }
        }

        /// <summary>
        /// 解析请求中的所有 Odin 固件包。
        /// </summary>
        /// <param name="request">刷入请求。</param>
        /// <returns>解析后的固件包集合。</returns>
        private List<HeimdallFirmwarePackageModel> ParsePackages(HeimdallFlashRequest request)
        {
            var packages = new List<HeimdallFirmwarePackageModel>();
            foreach (var firmwareFile in request.FirmwareFiles)
            {
                packages.Add(_firmwarePackageService.ParsePackage(firmwareFile.Key, firmwareFile.Value));
            }

            return packages;
        }

        /// <summary>
        /// 在 AP 包刷入前替换其 recovery 镜像，使完整 AP 流程写入 TWRP recovery。
        /// </summary>
        /// <param name="packages">已解析固件包集合。</param>
        /// <param name="request">刷入请求。</param>
        private void ApplyRecoveryOverride(IEnumerable<HeimdallFirmwarePackageModel> packages, HeimdallFlashRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RecoveryOverrideFilePath))
            {
                return;
            }

            var apPackage = packages.FirstOrDefault(item => item.Slot == HeimdallFirmwareSlot.AP);
            if (apPackage == null)
            {
                return;
            }

            request.Log?.Invoke("已选择 TWRP 镜像，正在替换 AP 包内的 recovery 镜像。");
            _firmwarePackageService.ApplyRecoveryOverride(apPackage, request.RecoveryOverrideFilePath);
        }

        /// <summary>
        /// 根据 PIT 条目和已解析固件文件构建刷入清单。
        /// </summary>
        /// <param name="packages">已解析固件包集合。</param>
        /// <param name="pitEntries">设备 PIT 分区条目集合。</param>
        /// <returns>待刷入文件清单。</returns>
        private static List<OdinSerialFlashItem> BuildFlashItems(
            IEnumerable<HeimdallFirmwarePackageModel> packages,
            IReadOnlyList<OdinPitEntryModel> pitEntries)
        {
            var result = new List<OdinSerialFlashItem>();
            foreach (var package in packages)
            {
                foreach (var firmwareFile in package.Files)
                {
                    var entry = FindPitEntry(firmwareFile, pitEntries);
                    if (entry == null)
                    {
                        continue;
                    }

                    result.Add(new OdinSerialFlashItem
                    {
                        FilePath = firmwareFile.ExtractedFilePath,
                        Entry = entry
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 为固件文件查找匹配的 PIT 分区条目。
        /// </summary>
        /// <param name="firmwareFile">固件文件。</param>
        /// <param name="pitEntries">PIT 分区条目集合。</param>
        /// <returns>匹配到的 PIT 条目，未匹配时返回 null。</returns>
        /// <summary>
        /// 记录本次 Odin 刷入文件与 PIT 分区的匹配结果，便于判断失败是否来自分区匹配或协议写入阶段。
        /// </summary>
        /// <param name="flashItems">待刷入的分区文件集合。</param>
        private static void LogFlashItems(IEnumerable<OdinSerialFlashItem> flashItems)
        {
            foreach (var item in flashItems)
            {
                var entry = item.Entry;
                HeimdallDebugLogService.Write(
                    "OdinSerial",
                    "FlashItem File=" + Path.GetFileName(item.FilePath) +
                    " Partition=" + entry.PartitionName +
                    " FileName=" + entry.FileName +
                    " BinaryType=" + entry.BinaryType +
                    " DeviceType=" + entry.DeviceType +
                    " PartitionId=" + entry.PartitionId +
                    " Size=" + new FileInfo(item.FilePath).Length);
            }
        }

        private static OdinPitEntryModel FindPitEntry(HeimdallFirmwareFileModel firmwareFile, IReadOnlyList<OdinPitEntryModel> pitEntries)
        {
            return pitEntries.FirstOrDefault(item =>
                       string.Equals(item.FileName, firmwareFile.EntryName, StringComparison.OrdinalIgnoreCase)) ??
                   pitEntries.FirstOrDefault(item =>
                       string.Equals(item.PartitionName, firmwareFile.SuggestedPartitionName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 格式化刷入进度文本。
        /// </summary>
        /// <param name="sent">已发送字节数。</param>
        /// <param name="total">总字节数。</param>
        /// <returns>进度文本。</returns>
        private static string FormatProgress(long sent, long total)
        {
            if (total <= 0)
            {
                return string.Empty;
            }

            return Math.Min(100d, sent * 100d / total).ToString("0.0") + "%";
        }

        /// <summary>
        /// 格式化任务详情中的刷入数据量。
        /// </summary>
        /// <param name="bytes">字节数。</param>
        /// <returns>便于阅读的数据量文本。</returns>
        private static string FormatBytes(long bytes)
        {
            const double unit = 1024d;
            if (bytes < unit)
            {
                return bytes + " B";
            }

            if (bytes < unit * unit)
            {
                return (bytes / unit).ToString("0.0") + " KB";
            }

            if (bytes < unit * unit * unit)
            {
                return (bytes / unit / unit).ToString("0.0") + " MB";
            }

            return (bytes / unit / unit / unit).ToString("0.0") + " GB";
        }

        /// <summary>
        /// 表示 Odin 串口后端中的单个待刷入分区文件。
        /// </summary>
        private sealed class OdinSerialFlashItem
        {
            /// <summary>
            /// 待刷入镜像文件路径。
            /// </summary>
            public string FilePath { get; set; } = string.Empty;

            /// <summary>
            /// 目标 PIT 分区条目。
            /// </summary>
            public OdinPitEntryModel Entry { get; set; }
        }
    }
}
