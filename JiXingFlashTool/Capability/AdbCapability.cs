using JiXingFlashTool.Enums;
using JiXingFlashTool.Extensions;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Model;
using JiXingFlashTool.Services;
using JXAdbCore.Receivers;
using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// 获取系统属性。
        /// </summary>
        /// <param name="propKey">属性键名。</param>
        /// <returns>属性值。</returns>
        public string GetProp(string propKey)
        {
            return AdbService.Instance.GetProp(_device, propKey);
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
            if (_device == null)
            {
                return JXAdbCore.Enums.DeviceState.Offline;
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
                throw new ArgumentException("刷入文件路径不能为空。", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("刷入文件不存在。", filePath);
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
                throw new ArgumentException("目标路径不能为空。", nameof(remotePath));
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
                throw new ArgumentException("资源名不能为空。", nameof(resourceName));
            }

            assembly ??= typeof(AdbCapability).Assembly;
            string manifestResourceName = ResolveEmbeddedResourceName(assembly, resourceName);
            using (var stream = assembly.GetManifestResourceStream(manifestResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"找不到嵌入资源: {resourceName}", resourceName);
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
                throw new ArgumentException("资源名不能为空。", nameof(resourceName));
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

            throw new InvalidOperationException($"APK 安装失败: {output}");
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

            throw new FileNotFoundException($"找不到嵌入资源: {resourceName}", resourceName);
        }
    }
}
