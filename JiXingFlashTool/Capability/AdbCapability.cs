using JiXingFlashTool.Enums;
using JiXingFlashTool.Extensions;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Model;
using JiXingFlashTool.Services;
using JiXingFlashTool.Utils;
using JXAdbCore.Receivers;
using LanguageCore;
using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JiXingFlashTool.Capability
{
    /// <summary>
    /// ADB 能力实现，负责把现有 AdbService 封装成 TaskCore 可调用的设备能力。
    /// </summary>
    public sealed class AdbCapability : IAdbCapability
    {
        private readonly DeviceModel _device;
        private SystemRootType? _rootType;

        /// <summary>
        /// 使用指定设备初始化 ADB 能力。
        /// </summary>
        /// <param name="device">当前设备。</param>
        public AdbCapability(DeviceModel device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>
        /// 执行同步 ADB 指令。
        /// </summary>
        /// <param name="command">指令内容。</param>
        /// <returns>指令输出。</returns>
        public string ExecuteRemoteCommand(string command)
        {
            var receiver = new ConsoleOutputReceiver();
            AdbService.Instance.ExecuteShellCommand(_device, command, receiver);
            return receiver.ToString();
        }

        /// <summary>
        /// 执行异步 ADB 指令。
        /// </summary>
        /// <param name="command">指令内容。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>指令输出。</returns>
        public string ExecuteRemoteCommand(string command, CancellationToken cancellationToken = default)
        {
            var receiver = new ConsoleOutputReceiver();
            AdbService.Instance.ExecuteShellCommand(_device, command, receiver);
            return receiver.ToString();
        }

        /// <summary>
        /// 获取系统属性；设备存在 su 时使用 Root 权限读取。
        /// </summary>
        /// <param name="propKey">属性键名。</param>
        /// <returns>属性值。</returns>
        public string GetProp(string propKey)
        {
            string escapedPropKey = EscapeShellArgument(propKey?.Trim());
            string command = string.IsNullOrWhiteSpace(escapedPropKey) ? "getprop" : $"getprop \"{escapedPropKey}\"";
            return ExecuteRemoteCommand(HasRemoteCommand("su") ? $"su -c {command}" : command);
        }

        /// <summary>
        /// 获取系统编译日期。
        /// </summary>
        /// <returns>格式为 yyyy-MM-dd 的系统编译日期；无法获取时返回空字符串。</returns>
        public string GetBuildDate()
        {
            string buildTimestamp = GetProp("ro.build.date.utc")?.Trim();
            if (!long.TryParse(buildTimestamp, out long unixTimestamp))
            {
                return string.Empty;
            }

            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).LocalDateTime.ToString("yyyy-MM-dd");
            }
            catch (ArgumentOutOfRangeException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 批量获取指定系统属性；设备存在 su 时使用 Root 权限读取，避免为每个属性重复执行 ADB Shell 指令。
        /// </summary>
        /// <param name="propKeys">属性键名集合。</param>
        /// <returns>属性键名与属性值的只读映射；不存在的属性值为空字符串。</returns>
        public IReadOnlyDictionary<string, string> GetProps(IEnumerable<string> propKeys)
        {
            var requestedKeys = new HashSet<string>(
                (propKeys ?? Enumerable.Empty<string>())
                    .Where(key => !string.IsNullOrWhiteSpace(key))
                    .Select(key => key.Trim()),
                StringComparer.Ordinal);
            var properties = requestedKeys.ToDictionary(key => key, key => string.Empty, StringComparer.Ordinal);
            if (properties.Count == 0)
            {
                return properties;
            }

            bool isSuAvailable = HasRemoteCommand("su");
            string output = ExecuteRemoteCommand(isSuAvailable ? "su -c getprop" : "getprop");
            foreach (Match match in Regex.Matches(output ?? string.Empty, @"^\[(?<key>[^\]]+)\]:\s\[(?<value>.*)\]$", RegexOptions.Multiline))
            {
                string key = match.Groups["key"].Value;
                if (properties.ContainsKey(key))
                {
                    properties[key] = match.Groups["value"].Value;
                }
            }

            return properties;
        }

        /// <summary>
        /// 获取当前设备 root 类型。
        /// </summary>
        /// <returns>root 类型。</returns>
        public SystemRootType GetRootType()
        {
            if (_rootType.HasValue)
            {
                return _rootType.Value;
            }

            if (_device?.RootType != SystemRootType.None)
            {
                _rootType = _device.RootType;
                return _rootType.Value;
            }

            var rootType = DetectRootType();
            _device.RootType = rootType;
            _rootType = rootType;
            return rootType;
        }

        /// <summary>
        /// 执行需要 root 权限的指令。
        /// </summary>
        /// <param name="command">原始指令。</param>
        /// <returns>执行结果。</returns>
        public string ExecuteRootCommand(string command)
        {
            string rootCommand = BuildRootCommand(command);
            if (string.IsNullOrWhiteSpace(rootCommand))
            {
                return string.Empty;
            }

            return ExecuteRemoteCommand(rootCommand);
        }

        /// <summary>
        /// 检查当前手机是否在线且可以正常执行 ADB 指令。
        /// </summary>
        /// <returns>在线返回 true。</returns>
        public bool IsDeviceOnline()
        {
            try
            {
                string output = ExecuteRemoteCommand("echo online");
                return string.Equals((output ?? string.Empty).Trim(), "online", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取当前设备所处状态。
        /// </summary>
        /// <returns>设备状态枚举。</returns>
        public JXAdbCore.Enums.DeviceState GetDeviceState()
        {
            return GetCurrentDeviceState();
        }

        /// <summary>
        /// 获取当前设备状态，并优先从 ADB 进程获取最新结果。
        /// </summary>
        public JXAdbCore.Enums.DeviceState GetCurrentDeviceState()
        {
            if (_device == null)
            {
                return JXAdbCore.Enums.DeviceState.Offline;
            }

            var stateFromAdbProcess = TryGetDeviceStateFromAdbProcess();
            if (stateFromAdbProcess != JXAdbCore.Enums.DeviceState.Unknown)
            {
                _device.State = stateFromAdbProcess;
                return stateFromAdbProcess;
            }

            try
            {
                var latestDevice = AdbService.Instance
                    .Devices()
                    .FirstOrDefault(item => item != null && string.Equals(item.Serial, _device.Serial, StringComparison.OrdinalIgnoreCase));

                if (latestDevice != null)
                {
                    _device.State = latestDevice.State;
                    return latestDevice.State;
                }
            }
            catch
            {
            }

            return _device.State;
        }

        /// <summary>
        /// 获取当前设备的展示信息。
        /// </summary>
        public CurrentDeviceInfoModel GetCurrentDeviceInfo()
        {
            var state = GetCurrentDeviceState();
            RefreshCurrentDeviceIdentityInfo(state);

            bool isEthernetConnection = !string.IsNullOrWhiteSpace(_device.Serial) && _device.Serial.Contains(":");
            string connectionTypeResourceKey = isEthernetConnection ? "Connection_Ethernet" : string.Empty;
            string stateResourceKey = GetDeviceStateResourceKey(state);
            return new CurrentDeviceInfoModel
            {
                Serial = _device.Serial ?? string.Empty,
                Brand = _device.Brand ?? string.Empty,
                Model = _device.Model ?? string.Empty,
                AndroidVersion = GetDisplayAndroidVersion(state),
                SystemVersion = _device.PolestarVersion ?? string.Empty,
                ConnectionType = isEthernetConnection ? GetLangText(connectionTypeResourceKey) : "USB",
                ConnectionTypeResourceKey = connectionTypeResourceKey,
                IsEthernetConnection = isEthernetConnection,
                State = state,
                StateText = string.IsNullOrWhiteSpace(stateResourceKey) ? "Download" : GetLangText(stateResourceKey),
                StateResourceKey = stateResourceKey
            };
        }

        /// <summary>
        /// 判断当前设备是否已经安装可正常开机的系统。
        /// </summary>
        /// <returns>已经安装且可正常开机返回 true。</returns>
        public bool HasBootableSystem()
        {
            if (_device == null)
            {
                return false;
            }

            var currentState = GetDeviceState();
            if (currentState != JXAdbCore.Enums.DeviceState.Online &&
                currentState != JXAdbCore.Enums.DeviceState.Recovery &&
                currentState != JXAdbCore.Enums.DeviceState.Sideload)
            {
                return false;
            }

            string systemCheck = ExecuteRemoteCommand("if [ -d /system ] || [ -d /system/system ] || [ -d /system_ext ]; then echo 1; else echo 0; fi");
            if (!string.Equals(systemCheck?.Trim(), "1", StringComparison.Ordinal))
            {
                return false;
            }

            string[] bootCandidates =
            {
                "/dev/block/bootdevice/by-name/boot",
                "/dev/block/platform/soc/1d84000.ufshc/by-name/boot",
                "/dev/block/platform/11120000.ufs/by-name/boot",
                "/dev/block/platform/15570000.ufs/by-name/boot",
                "/dev/block/platform/155a0000.ufs/by-name/boot"
            };

            return bootCandidates.Any(path => AdbService.Instance.FileExist(_device, path));
        }

        /// <summary>
        /// 恢复局域网设备连接；仅在设备 IP 可 Ping 通时执行 ADB 重连。
        /// </summary>
        public bool RestoreConnection(CancellationToken cancellationToken = default)
        {
            if (!TryCreateNetworkEndpoint(_device.Serial, out DnsEndPoint endpoint))
            {
                return false;
            }

            while (true)
            {
                if (!CanPingDevice(endpoint.Host, cancellationToken))
                {
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                var currentDevice = FindCurrentDeviceInAdbList();
                if (currentDevice != null && currentDevice.State != JXAdbCore.Enums.DeviceState.Offline)
                {
                    _device.State = currentDevice.State;
                    return true;
                }

                try
                {
                    if (currentDevice != null && currentDevice.State == JXAdbCore.Enums.DeviceState.Offline)
                    {
                        AdbService.Instance.Disconnect(endpoint);
                    }

                    if (AdbService.Instance.Connect(endpoint))
                    {
                        WaitForConnectionRetry(cancellationToken);
                        currentDevice = FindCurrentDeviceInAdbList();
                        if (currentDevice != null && currentDevice.State != JXAdbCore.Enums.DeviceState.Offline)
                        {
                            _device.State = currentDevice.State;
                            return true;
                        }
                    }
                }
                catch
                {
                }

                WaitForConnectionRetry(cancellationToken);
            }
        }

        /// <summary>
        /// 异步执行需要 root 权限的指令。
        /// </summary>
        /// <param name="command">原始指令。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>异步任务。</returns>
        public Task ExecuteRootCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                string rootCommand = BuildRootCommand(command);
                if (!string.IsNullOrWhiteSpace(rootCommand))
                {
                    AdbService.Instance.ExecuteShellCommand(_device, rootCommand, null);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 推送本地文件到设备。
        /// </summary>
        /// <param name="filePath">本地文件路径。</param>
        /// <param name="remotePath">设备目标路径。</param>
        /// <param name="progress">进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public void PushLocalFile(string filePath, string remotePath, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空。", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("本地文件不存在。", filePath);
            }

            using (var stream = File.OpenRead(filePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AdbService.Instance.Push(_device, stream, remotePath, progress, cancellationToken);
            }
        }

        /// <summary>
        /// 通过 adb 进程推送本地文件，并返回传输是否成功。
        /// </summary>
        public void PushLocalFileByAdbProcess(string filePath, string remotePath, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            ValidateLocalPushFile(filePath, remotePath);

            string arguments = $"-s {EscapeProcessArgument(_device.Serial)} push {EscapeProcessArgument(filePath)} {EscapeProcessArgument(remotePath)}";
            int lastReportedProgress = -1;
            string output = RunAdbProcess(arguments, line =>
            {
                int percentage = ReadProgressPercentage(line);
                if (percentage < 0 || percentage == lastReportedProgress)
                {
                    return;
                }

                lastReportedProgress = percentage;
                progress?.Report(percentage);
            }, cancellationToken);

            if (output.IndexOf("error:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                output.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException($"ADB 闂傚倷绀侀幖顐﹀磹缁嬫５娲晲閸涱亝鐎婚梺闈涚箞閸婃洜绮婚幎鑺ョ厪濠电偟鍋撻弶褰掓煕鐎ｎ偅灏伴柟宄版嚇楠炴牠鎮欓悧鍫熺窔闂? {output}");
            }

            progress?.Report(100);
        }

        /// <summary>
        /// 执行侧载刷入指令。
        /// </summary>
        /// <param name="filePath">本地刷入文件路径。</param>
        /// <param name="progress">数字进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>指令输出。</returns>
        public string SideloadFile(string filePath, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空。", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("本地文件不存在。", filePath);
            }

            cancellationToken.ThrowIfCancellationRequested();

            string escapedFilePath = EscapeShellArgument(filePath);
            string command = $"sideload \"{escapedFilePath}\"";
            int lastReportedProgress = -1;
            string output = AdbService.Instance.CMDExcute($"-s {_device.Serial} {command}", line =>
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                string percentageText = line.MatchedPercentage();
                if (string.IsNullOrWhiteSpace(percentageText))
                {
                    return;
                }

                if (!int.TryParse(percentageText.TrimEnd('%'), out int percentage))
                {
                    return;
                }

                if (percentage == lastReportedProgress)
                {
                    return;
                }

                lastReportedProgress = percentage;
                progress?.Report(percentage);
            });

            return output ?? string.Empty;
        }

        /// <summary>
        /// 安装本地 APK 文件。
        /// </summary>
        /// <param name="apkFilePath">本地 APK 文件路径。</param>
        /// <param name="useRoot">是否使用 root 权限执行安装。</param>
        /// <param name="progress">进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public void InstallApkFromLocalFile(string apkFilePath, bool useRoot = false, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(apkFilePath))
            {
                throw new ArgumentException("APK 文件路径不能为空。", nameof(apkFilePath));
            }

            if (!File.Exists(apkFilePath))
            {
                throw new FileNotFoundException("APK 文件不存在。", apkFilePath);
            }

            string tempRemotePath = "/data/local/tmp/" + Path.GetFileName(apkFilePath);
            PushLocalFile(apkFilePath, tempRemotePath, progress, cancellationToken);
            InstallApkFromRemotePath(tempRemotePath, useRoot, cancellationToken);
        }

        /// <summary>
        /// 将文本内容写入设备指定路径。
        /// </summary>
        /// <param name="content">要写入的文本内容。</param>
        /// <param name="remotePath">设备端目标路径。</param>
        /// <param name="progress">进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public void WriteTextToRemoteFile(string content, string remotePath, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            WriteTextToRemoteFile(content, remotePath, false, progress, cancellationToken);
        }

        /// <summary>
        /// 将文本内容写入设备指定路径，并允许指定是否使用 root 权限。
        /// </summary>
        /// <param name="content">要写入的文本内容。</param>
        /// <param name="remotePath">设备端目标路径。</param>
        /// <param name="useRoot">是否使用 root 权限。</param>
        /// <param name="progress">进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public void WriteTextToRemoteFile(string content, string remotePath, bool useRoot, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentException("设备目标路径不能为空。", nameof(remotePath));
            }

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
            try
            {
                File.WriteAllText(tempFilePath, content ?? string.Empty, Encoding.UTF8);

                if (!useRoot)
                {
                    PushLocalFile(tempFilePath, remotePath, progress, cancellationToken);
                    return;
                }

                string tempRemotePath = "/data/local/tmp/" + Guid.NewGuid().ToString("N") + ".txt";
                PushLocalFile(tempFilePath, tempRemotePath, progress, cancellationToken);

                string escapedTempPath = EscapeShellArgument(tempRemotePath);
                string escapedRemotePath = EscapeShellArgument(remotePath);
                ExecuteRootCommand($"cat \"{escapedTempPath}\" > \"{escapedRemotePath}\"");
                ExecuteRemoteCommand($"rm -f \"{escapedTempPath}\"");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        /// <summary>
        /// 推送程序集嵌入资源到设备。
        /// </summary>
        /// <param name="resourceName">嵌入资源名。</param>
        /// <param name="remotePath">设备目标路径。</param>
        /// <param name="assembly">资源所在程序集。</param>
        /// <param name="progress">进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public void PushEmbeddedResource(string resourceName, string remotePath, Assembly assembly = null, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentException("嵌入资源名称不能为空。", nameof(resourceName));
            }

            assembly ??= typeof(AdbCapability).Assembly;
            string manifestResourceName = ResolveEmbeddedResourceName(assembly, resourceName);
            using (var stream = assembly.GetManifestResourceStream(manifestResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"闂傚倷绀佺紞濠傤焽瑜戦妵鎰版倷閻㈢數鐣舵繝銏ｅ煐閸旀洜绮堥崱娑欑厸濠㈣泛瀛╃涵鍫曟煏閸垺鏆柡灞剧缁犳稓鈧綆浜滄慨銈囩磽娴ｉ璐伴柛瀣閸? {resourceName}", resourceName);
                }

                cancellationToken.ThrowIfCancellationRequested();
                AdbService.Instance.Push(_device, stream, remotePath, progress, cancellationToken);
            }
        }

        /// <summary>
        /// 安装嵌入资源中的 APK 文件。
        /// </summary>
        /// <param name="resourceName">嵌入资源名。</param>
        /// <param name="assembly">资源所在程序集。</param>
        /// <param name="useRoot">是否使用 root 权限执行安装。</param>
        /// <param name="progress">进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public void InstallApkFromEmbeddedResource(string resourceName, Assembly assembly = null, bool useRoot = false, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentException("嵌入资源名称不能为空。", nameof(resourceName));
            }

            assembly ??= typeof(AdbCapability).Assembly;
            string manifestResourceName = ResolveEmbeddedResourceName(assembly, resourceName);
            string tempRemotePath = "/data/local/tmp/" + Path.GetFileName(resourceName);
            PushEmbeddedResource(manifestResourceName, tempRemotePath, assembly, progress, cancellationToken);
            InstallApkFromRemotePath(tempRemotePath, useRoot, cancellationToken);
        }

        /// <summary>
        /// 检查设备端文件是否存在。
        /// </summary>
        /// <param name="remotePath">设备文件路径。</param>
        /// <param name="useRoot">是否使用 root 权限。</param>
        /// <returns>存在返回 true。</returns>
        public bool CheckRemoteFileExists(string remotePath, bool useRoot = false)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                return false;
            }

            string escapedPath = EscapeShellArgument(remotePath);
            string command = $"if [ -e \"{escapedPath}\" ]; then echo 1; else echo 0; fi";
            string output = useRoot ? ExecuteRootCommand(command) : ExecuteRemoteCommand(command);
            return string.Equals((output ?? string.Empty).Trim(), "1", StringComparison.Ordinal);
        }

        /// <summary>
        /// 移除设备端指定文件。
        /// </summary>
        /// <param name="remotePath">设备文件路径。</param>
        /// <param name="useRoot">是否使用 root 权限。</param>
        public void RemoveRemoteFile(string remotePath, bool useRoot = false)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentException("设备目标路径不能为空。", nameof(remotePath));
            }

            string escapedPath = EscapeShellArgument(remotePath);
            string command = $"rm -f \"{escapedPath}\"";
            if (useRoot)
            {
                ExecuteRootCommand(command);
                return;
            }

            ExecuteRemoteCommand(command);
        }

        /// <summary>
        /// 获取设备端可用的 ext4 格式化指令名。
        /// </summary>
        /// <returns>返回可用指令名；都不存在则返回空字符串。</returns>
        public string GetAvailableExt4FormatCommand()
        {
            if (HasRemoteCommand("make_ext4fs"))
            {
                return "make_ext4fs";
            }

            if (HasRemoteCommand("mke2fs"))
            {
                return "mke2fs";
            }

            return string.Empty;
        }

        /// <summary>
        /// 检查设备端是否存在指定 shell 指令。
        /// </summary>
        /// <param name="commandName">指令名称。</param>
        /// <returns>存在返回 true。</returns>
        private bool HasRemoteCommand(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                return false;
            }

            string escapedCommandName = EscapeShellArgument(commandName.Trim());
            string command = $"if command -v \"{escapedCommandName}\" >/dev/null 2>&1; then echo 1; else echo 0; fi";
            string output = ExecuteRemoteCommand(command);
            return string.Equals((output ?? string.Empty).Trim(), "1", StringComparison.Ordinal);
        }

        /// <summary>
        /// 比较设备文件与本地文件的 MD5 是否一致。
        /// </summary>
        /// <param name="remotePath">设备文件路径。</param>
        /// <param name="localFilePath">本地文件路径。</param>
        /// <param name="useRoot">是否使用 root 权限。</param>
        /// <returns>一致返回 true。</returns>
        public bool IsRemoteFileMd5EqualToLocalFile(string remotePath, string localFilePath, bool useRoot = false)
        {
            if (string.IsNullOrWhiteSpace(localFilePath) || !File.Exists(localFilePath))
            {
                return false;
            }

            string localMd5 = ComputeFileMd5(localFilePath);
            string remoteMd5 = GetRemoteFileMd5(remotePath, useRoot);
            return !string.IsNullOrWhiteSpace(localMd5) && localMd5.Equals(remoteMd5, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 比较远端文件与本地文件的大小是否一致。
        /// </summary>
        public bool IsRemoteFileSizeEqualToLocalFile(string remotePath, string localFilePath, bool useRoot = false)
        {
            if (string.IsNullOrWhiteSpace(localFilePath) || !File.Exists(localFilePath))
            {
                return false;
            }

            long remoteFileSize = GetRemoteFileSize(remotePath, useRoot);
            long localFileSize = new FileInfo(localFilePath).Length;
            return remoteFileSize >= 0 && remoteFileSize == localFileSize;
        }

        /// <summary>
        /// 比较设备文件与嵌入资源的 MD5 是否一致。
        /// </summary>
        /// <param name="remotePath">设备文件路径。</param>
        /// <param name="resourceName">嵌入资源名。</param>
        /// <param name="assembly">资源所在程序集。</param>
        /// <param name="useRoot">是否使用 root 权限。</param>
        /// <returns>一致返回 true。</returns>
        public bool IsRemoteFileMd5EqualToEmbeddedResource(string remotePath, string resourceName, Assembly assembly = null, bool useRoot = false)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return false;
            }

            assembly ??= typeof(AdbCapability).Assembly;
            string manifestResourceName = ResolveEmbeddedResourceName(assembly, resourceName);
            using (var stream = assembly.GetManifestResourceStream(manifestResourceName))
            {
                if (stream == null)
                {
                    return false;
                }

                string resourceMd5 = ComputeStreamMd5(stream);
                string remoteMd5 = GetRemoteFileMd5(remotePath, useRoot);
                return !string.IsNullOrWhiteSpace(resourceMd5) && resourceMd5.Equals(remoteMd5, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// 检查设备指定路径中的文本内容是否与期望内容一致。
        /// </summary>
        /// <param name="remotePath">设备文件路径。</param>
        /// <param name="expectedContent">期望的文本内容。</param>
        /// <param name="useRoot">是否使用 root 权限读取。</param>
        /// <returns>一致返回 true。</returns>
        public bool IsRemoteFileTextEqual(string remotePath, string expectedContent, bool useRoot = false)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                return false;
            }

            string escapedPath = EscapeShellArgument(remotePath);
            string output = useRoot
                ? ExecuteRootCommand($"cat \"{escapedPath}\"")
                : ExecuteRemoteCommand($"cat \"{escapedPath}\"");

            return string.Equals(output?.Replace("\r\n", "\n").Trim(), (expectedContent ?? string.Empty).Replace("\r\n", "\n").Trim(), StringComparison.Ordinal);
        }

        /// <summary>
        /// 获取设备中已安装的软件包名。
        /// </summary>
        /// <param name="includeSystemPackages">是否包含系统应用包名。</param>
        /// <returns>已安装的软件包名列表。</returns>
        public IReadOnlyList<string> GetInstalledPackageNames(bool includeSystemPackages = false)
        {
            string command = includeSystemPackages ? "pm list packages" : "pm list packages -3";
            string output = ExecuteRemoteCommand(command);

            if (string.IsNullOrWhiteSpace(output))
            {
                return Array.Empty<string>();
            }

            return output
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("package:", StringComparison.OrdinalIgnoreCase))
                .Select(line => line.Substring("package:".Length).Trim())
                .Where(packageName => !string.IsNullOrWhiteSpace(packageName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// 根据包名检查应用是否已安装。
        /// </summary>
        /// <param name="packageName">包名。</param>
        /// <returns>已安装返回 true。</returns>
        public bool IsAppInstalled(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return false;
            }

            return GetInstalledPackageNames(true)
                .Any(name => string.Equals(name, packageName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 检测当前设备 root 类型。
        /// </summary>
        /// <returns>root 类型。</returns>
        private SystemRootType DetectRootType()
        {
            try
            {
                string shellRootResult = ExecuteRemoteCommand("id");
                if (shellRootResult.IndexOf("uid=0", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return SystemRootType.Shell;
                }

                string suResult = ExecuteRemoteCommand("su -c id");
                if (suResult.IndexOf("uid=0", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return SystemRootType.Kernelsu;
                }
            }
            catch
            {
            }

            return SystemRootType.None;
        }

        /// <summary>
        /// 从当前 ADB 设备列表中查找目标设备。
        /// </summary>
        private DeviceModel FindCurrentDeviceInAdbList()
        {
            if (string.IsNullOrWhiteSpace(_device.Serial))
            {
                return null;
            }

            try
            {
                return AdbService.Instance
                    .Devices()
                    .FirstOrDefault(item => item != null && string.Equals(item.Serial, _device.Serial, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 将设备序列号解析为网络端点。
        /// </summary>
        private static bool TryCreateNetworkEndpoint(string serial, out DnsEndPoint endpoint)
        {
            endpoint = null;
            if (string.IsNullOrWhiteSpace(serial))
            {
                return false;
            }

            string[] parts = serial.Split(':');
            if (parts.Length != 2 ||
                !IPAddress.TryParse(parts[0], out _) ||
                !int.TryParse(parts[1], out int port))
            {
                return false;
            }

            endpoint = new DnsEndPoint(parts[0], port);
            return true;
        }

        /// <summary>
        /// 等待下一次连接重试。
        /// </summary>
        private static void WaitForConnectionRetry(CancellationToken cancellationToken)
        {
            if (cancellationToken.WaitHandle.WaitOne(1000))
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// 检测目标设备 IP 是否可以 Ping 通。
        /// </summary>
        private static bool CanPingDevice(string host, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using (var ping = new Ping())
                {
                    PingReply reply = ping.Send(host, 1000);
                    return reply != null && reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 通过 adb devices 命令获取设备状态。
        /// </summary>
        private JXAdbCore.Enums.DeviceState TryGetDeviceStateFromAdbProcess()
        {
            if (string.IsNullOrWhiteSpace(_device.Serial))
            {
                return JXAdbCore.Enums.DeviceState.Unknown;
            }

            try
            {
                string output = RunAdbProcess("devices -l", null, CancellationToken.None);
                string[] lines = (output ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (trimmedLine.Length == 0 ||
                        trimmedLine.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase) ||
                        !trimmedLine.StartsWith(_device.Serial, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string[] parts = Regex.Split(trimmedLine, @"\s+");
                    if (parts.Length >= 2 && string.Equals(parts[0], _device.Serial, StringComparison.OrdinalIgnoreCase))
                    {
                        return ConvertAdbStateText(parts[1]);
                    }
                }
            }
            catch
            {
            }

            return JXAdbCore.Enums.DeviceState.Unknown;
        }

        /// <summary>
        /// 刷新设备品牌、型号和系统版本等身份信息。
        /// </summary>
        private void RefreshCurrentDeviceIdentityInfo(JXAdbCore.Enums.DeviceState state)
        {
            if (_device == null)
            {
                return;
            }

            _device.Brand = GetCachedOrPropValue(_device.Brand, "ro.product.brand");
            _device.Name = GetCachedOrPropValue(_device.Name, "ro.product.device");

            if (state == JXAdbCore.Enums.DeviceState.Online)
            {
                _device.AndroidVersion = GetCachedOrPropValue(_device.AndroidVersion, "ro.build.version.release");
                _device.PolestarVersion = GetCachedOrRootPropValue(_device.PolestarVersion, "ro.polestar.system.version");
            }
            else if (state == JXAdbCore.Enums.DeviceState.Recovery)
            {
                _device.TWRPVersion = GetCachedOrPropValue(_device.TWRPVersion, "ro.twrp.version");
            }
        }

        /// <summary>
        /// 获取缓存值或读取普通系统属性。
        /// </summary>
        private string GetCachedOrPropValue(string cachedValue, string propKey)
        {
            if (!string.IsNullOrWhiteSpace(cachedValue))
            {
                return cachedValue;
            }

            return ReadDeviceProp(propKey);
        }

        /// <summary>
        /// 获取缓存值或读取需要 Root 权限的系统属性。
        /// </summary>
        private string GetCachedOrRootPropValue(string cachedValue, string propKey)
        {
            if (!string.IsNullOrWhiteSpace(cachedValue))
            {
                return cachedValue;
            }

            string rootValue = ReadRootDeviceProp(propKey);
            return string.IsNullOrWhiteSpace(rootValue) ? ReadDeviceProp(propKey) : rootValue;
        }

        /// <summary>
        /// 读取普通系统属性。
        /// </summary>
        private string ReadDeviceProp(string propKey)
        {
            try
            {
                return (GetProp(propKey) ?? string.Empty).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 读取需要 Root 权限的系统属性。
        /// </summary>
        private string ReadRootDeviceProp(string propKey)
        {
            try
            {
                return (ExecuteRootCommand($"getprop {propKey}") ?? string.Empty).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取用于界面展示的 Android 版本。
        /// </summary>
        private string GetDisplayAndroidVersion(JXAdbCore.Enums.DeviceState state)
        {
            if (state == JXAdbCore.Enums.DeviceState.Recovery)
            {
                return _device.TWRPVersion ?? string.Empty;
            }

            return state == JXAdbCore.Enums.DeviceState.Online ? _device.AndroidVersion ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// 获取设备状态对应的多语言资源键。
        /// </summary>
        private static string GetDeviceStateResourceKey(JXAdbCore.Enums.DeviceState state)
        {
            switch (state)
            {
                case JXAdbCore.Enums.DeviceState.Online:
                    return "DeviceState_System";
                case JXAdbCore.Enums.DeviceState.Recovery:
                    return "DeviceState_Recovery";
                case JXAdbCore.Enums.DeviceState.BootLoader:
                    return string.Empty;
                case JXAdbCore.Enums.DeviceState.Sideload:
                    return "DeviceState_Sideload";
                case JXAdbCore.Enums.DeviceState.Offline:
                    return "DeviceState_Offline";
                case JXAdbCore.Enums.DeviceState.Unauthorized:
                    return "DeviceState_Unauthorized";
                case JXAdbCore.Enums.DeviceState.Authorizing:
                    return "DeviceState_Verifying";
                case JXAdbCore.Enums.DeviceState.NoPermissions:
                    return "DeviceState_NoPermission";
                case JXAdbCore.Enums.DeviceState.Host:
                    return "DeviceState_NetworkMode";
                default:
                    return "DeviceState_Unknown";
            }
        }

        /// <summary>
        /// 读取指定资源键的当前语言文本。
        /// </summary>
        private static string GetLangText(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : LocalizationService.Instance.GetString(string.Empty, key);
        }

        /// <summary>
        /// 将 adb 状态文本转换为设备状态枚举。
        /// </summary>
        private static JXAdbCore.Enums.DeviceState ConvertAdbStateText(string stateText)
        {
            switch ((stateText ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "device":
                    return JXAdbCore.Enums.DeviceState.Online;
                case "recovery":
                    return JXAdbCore.Enums.DeviceState.Recovery;
                case "sideload":
                    return JXAdbCore.Enums.DeviceState.Sideload;
                case "download":
                case "bootloader":
                    return JXAdbCore.Enums.DeviceState.BootLoader;
                case "offline":
                    return JXAdbCore.Enums.DeviceState.Offline;
                case "unauthorized":
                    return JXAdbCore.Enums.DeviceState.Unauthorized;
                case "authorizing":
                    return JXAdbCore.Enums.DeviceState.Authorizing;
                case "no permissions":
                    return JXAdbCore.Enums.DeviceState.NoPermissions;
                case "host":
                    return JXAdbCore.Enums.DeviceState.Host;
                default:
                    return JXAdbCore.Enums.DeviceState.Unknown;
            }
        }

        /// <summary>
        /// 根据 root 类型构造可执行指令。
        /// </summary>
        /// <param name="command">原始指令。</param>
        /// <returns>可执行指令。</returns>
        private string BuildRootCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return string.Empty;
            }

            var rootType = GetRootType();
            switch (rootType)
            {
                case SystemRootType.Shell:
                    return command;
                case SystemRootType.Kernelsu:
                    return $"su -c \"{command.Replace("\"", "\\\"")}\"";
                default:
                    throw new InvalidOperationException("当前设备没有可用的 root 权限。");
            }
        }

        /// <summary>
        /// 从设备临时路径执行 APK 安装。
        /// </summary>
        /// <param name="remoteApkPath">设备端 APK 路径。</param>
        /// <param name="useRoot">是否使用 root 权限。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        private void InstallApkFromRemotePath(string remoteApkPath, bool useRoot, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string escapedPath = EscapeShellArgument(remoteApkPath);
            string installCommand = $"pm install -r \"{escapedPath}\"";
            string output = useRoot ? ExecuteRootCommand(installCommand) : ExecuteRemoteCommand(installCommand);

            if (!string.IsNullOrWhiteSpace(output) &&
                output.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            throw new InvalidOperationException($"APK 闂備浇顕уù鐑藉箠閹惧嚢鍥箮閸撳灝顦扮换婵嬪礃閵娧呯嵁闂佸搫顦悧婊堝磻婢跺寒鏉? {output}");
        }

        /// <summary>
        /// 获取设备文件的 MD5。
        /// </summary>
        /// <param name="remotePath">设备文件路径。</param>
        /// <param name="useRoot">是否使用 root 权限。</param>
        /// <returns>MD5 字符串。</returns>
        private string GetRemoteFileMd5(string remotePath, bool useRoot)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                return string.Empty;
            }

            string escapedPath = EscapeShellArgument(remotePath);
            string command = $"md5sum \"{escapedPath}\"";
            string output = useRoot ? ExecuteRootCommand(command) : ExecuteRemoteCommand(command);
            Match match = Regex.Match(output ?? string.Empty, @"([a-fA-F0-9]{32})");
            return match.Success ? match.Groups[1].Value.ToLowerInvariant() : string.Empty;
        }

        /// <summary>
        /// 获取远端文件大小。
        /// </summary>
        private long GetRemoteFileSize(string remotePath, bool useRoot)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                return -1;
            }

            string escapedPath = EscapeShellArgument(remotePath);
            string command = $"wc -c < \"{escapedPath}\"";
            string output = useRoot ? ExecuteRootCommand(command) : ExecuteRemoteCommand(command);
            Match match = Regex.Match(output ?? string.Empty, @"\d+");
            return match.Success && long.TryParse(match.Value, out long fileSize) ? fileSize : -1;
        }

        /// <summary>
        /// 计算本地文件 MD5。
        /// </summary>
        /// <param name="filePath">本地文件路径。</param>
        /// <returns>MD5 字符串。</returns>
        private static string ComputeFileMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                return ComputeMd5(md5, stream);
            }
        }

        /// <summary>
        /// 计算流的 MD5。
        /// </summary>
        /// <param name="stream">输入流。</param>
        /// <returns>MD5 字符串。</returns>
        private static string ComputeStreamMd5(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                return ComputeMd5(md5, stream);
            }
        }

        /// <summary>
        /// 计算 MD5 并转换为小写十六进制字符串。
        /// </summary>
        /// <param name="md5">MD5 实例。</param>
        /// <param name="stream">输入流。</param>
        /// <returns>MD5 字符串。</returns>
        private static string ComputeMd5(MD5 md5, Stream stream)
        {
            byte[] hash = md5.ComputeHash(stream);
            var builder = new StringBuilder(hash.Length * 2);
            foreach (byte item in hash)
            {
                builder.Append(item.ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// 转义 shell 参数中的引号。
        /// </summary>
        /// <param name="argument">原始参数。</param>
        /// <returns>转义后的参数。</returns>
        private static string EscapeShellArgument(string argument)
        {
            return argument?.Replace("\"", "\\\"") ?? string.Empty;
        }

        /// <summary>
        /// 校验本地待推送文件是否有效。
        /// </summary>
        private static void ValidateLocalPushFile(string filePath, string remotePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空。", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("本地文件不存在。", filePath);
            }

            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentException("设备目标路径不能为空。", nameof(remotePath));
            }
        }

        /// <summary>
        /// 执行 adb 进程并获取输出。
        /// </summary>
        private static string RunAdbProcess(string arguments, Action<string> outputLineReceived, CancellationToken cancellationToken)
        {
            var outputBuilder = new StringBuilder();
            using (var process = new Process())
            {
                process.StartInfo.FileName = StaticConstant.PathAdb;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.OutputDataReceived += (sender, args) => AppendAdbOutput(outputBuilder, outputLineReceived, args.Data);
                process.ErrorDataReceived += (sender, args) => AppendAdbOutput(outputBuilder, outputLineReceived, args.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                while (!process.WaitForExit(200))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        continue;
                    }

                    TryKillProcess(process);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (process.ExitCode == 0)
                {
                    return outputBuilder.ToString();
                }

                throw new InvalidOperationException($"ADB 闂傚倷绀侀幉锛勭矙閹烘鍨傛繝闈涱儏缁狀噣鏌曢崼婵愭Ц缂佺姰鍎抽幉鎼佸箣閿旇　鍋撴笟鈧獮瀣倷閼碱剛鐛梺鍝勵槸閻楁粓宕戞径搴澓闂傚倷鐒︾€笛呯矙閹达附鍎楅柛宀€鍋涢悞鍨亜閹达絾顥夊ù婊堢畺濮婃椽宕ㄦ繝鍌滀紘濠电偛鎷戠紞渚€骞? {process.ExitCode}闂傚倷鐒︾€笛呯矙閹达附鍎楀ù锝囧劋椤愯姤銇勯幘鍗炵仼缂佲偓? {outputBuilder}");
            }
        }

        /// <summary>
        /// 追加 adb 进程输出内容。
        /// </summary>
        private static void AppendAdbOutput(StringBuilder outputBuilder, Action<string> outputLineReceived, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            outputBuilder.AppendLine(line);
            outputLineReceived?.Invoke(line);
        }

        /// <summary>
        /// 从 adb 输出中读取传输进度。
        /// </summary>
        private static int ReadProgressPercentage(string line)
        {
            Match match = Regex.Match(line ?? string.Empty, @"(\d+)%");
            return match.Success && int.TryParse(match.Groups[1].Value, out int percentage) ? percentage : -1;
        }

        /// <summary>
        /// 转义 adb 进程参数。
        /// </summary>
        private static string EscapeProcessArgument(string argument)
        {
            return "\"" + (argument ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// 尝试终止超时的 adb 进程。
        /// </summary>
        private static void TryKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 根据简写资源名解析真实嵌入资源名。
        /// </summary>
        /// <param name="assembly">资源所在程序集。</param>
        /// <param name="resourceName">传入的资源名。</param>
        /// <returns>真实的 manifest 资源名。</returns>
        private static string ResolveEmbeddedResourceName(Assembly assembly, string resourceName)
        {
            string[] resourceNames = assembly.GetManifestResourceNames();

            string exactMatch = resourceNames.FirstOrDefault(name => string.Equals(name, resourceName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(exactMatch))
            {
                return exactMatch;
            }

            string suffixMatch = resourceNames.FirstOrDefault(name => name.EndsWith("." + resourceName, StringComparison.OrdinalIgnoreCase) || name.EndsWith("/" + resourceName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(suffixMatch))
            {
                return suffixMatch;
            }

            throw new FileNotFoundException($"闂傚倷绀佺紞濠傤焽瑜戦妵鎰版倷閻㈢數鐣舵繝銏ｅ煐閸旀洜绮堥崱娑欑厸濠㈣泛瀛╃涵鍫曟煏閸垺鏆柡灞剧缁犳稓鈧綆浜滄慨銈囩磽娴ｉ璐伴柛瀣閸? {resourceName}", resourceName);
        }
    }
}
