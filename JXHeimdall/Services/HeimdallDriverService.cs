using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXHeimdall.Services
{
    /// <summary>
    /// 负责为 Samsung Download 设备自动安装 Heimdall 所需的 WinUSB 驱动。
    /// </summary>
    public sealed class HeimdallDriverService
    {
        private const string SamsungVendorId = "0x04E8";
        private const string SamsungDownloadProductId = "0x685D";
        private const string SamsungDownloadHardwareId = "USB\\VID_04E8&PID_685D";
        private const string ExactInfName = "samsung_download_winusb_exact.inf";
        private readonly string _driverInstallerPath;
        private readonly string _driverPackageDirectory;
        private readonly string _exactInfPath;

        /// <summary>
        /// 初始化 Heimdall 驱动服务。
        /// </summary>
        public HeimdallDriverService()
        {
            _driverInstallerPath = ResolveDriverInstallerPath();
            _driverPackageDirectory = Path.Combine(Path.GetDirectoryName(_driverInstallerPath), "Driver", "SamsungDownloadWinUSB");
            _exactInfPath = Path.Combine(_driverPackageDirectory, ExactInfName);
        }

        /// <summary>
        /// 判断随包驱动安装工具是否存在。
        /// </summary>
        public bool IsAvailable => File.Exists(_driverInstallerPath);

        /// <summary>
        /// 自动修复 Samsung Download 设备的 WinUSB 驱动绑定。
        /// </summary>
        /// <param name="log">任务详情日志回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>驱动修复流程退出码，0 表示命令流程执行成功。</returns>
        public async Task<int> InstallSamsungDownloadWinUsbAsync(Action<string> log, CancellationToken cancellationToken)
        {
            if (!IsAvailable)
            {
                throw new FileNotFoundException("未找到 WinUSB 驱动安装工具。", _driverInstallerPath);
            }

            log?.Invoke("正在检查 Samsung Download 的 Windows 驱动...");
            HeimdallDebugLogService.Write("Driver", "Installer=" + _driverInstallerPath);
            await LogCurrentDeviceStateAsync("before", cancellationToken).ConfigureAwait(false);

            var exitCode = IsAdministrator()
                ? await RepairDriverAsAdministratorAsync(log, cancellationToken).ConfigureAwait(false)
                : await RunElevatedRepairScriptAsync(log, cancellationToken).ConfigureAwait(false);

            await Task.Delay(1500, cancellationToken).ConfigureAwait(false);
            await LogCurrentDeviceStateAsync("after", cancellationToken).ConfigureAwait(false);

            if (exitCode == 0)
            {
                log?.Invoke("WinUSB 驱动修复完成，正在重新读取 PIT。");
            }
            else
            {
                log?.Invoke("WinUSB 驱动修复未完成，退出码：" + exitCode);
            }

            return exitCode;
        }

        /// <summary>
        /// 在管理员权限下执行完整驱动修复流程。
        /// </summary>
        /// <param name="log">任务详情日志回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>最后一个失败命令的退出码，全部成功时返回 0。</returns>
        private async Task<int> RepairDriverAsAdministratorAsync(Action<string> log, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_driverPackageDirectory);

            var extractArguments = BuildExtractArguments();
            var extractResult = await RunProcessAsync(_driverInstallerPath, extractArguments, cancellationToken).ConfigureAwait(false);
            LogCommandResult("wdi-extract", extractResult);
            if (extractResult.ExitCode != 0 || !File.Exists(_exactInfPath))
            {
                return extractResult.ExitCode == 0 ? -1 : extractResult.ExitCode;
            }

            var addDriverResult = await RunProcessAsync("pnputil.exe", "/add-driver " + Quote(_exactInfPath) + " /install", cancellationToken).ConfigureAwait(false);
            LogCommandResult("pnputil-add-driver", addDriverResult);
            if (addDriverResult.ExitCode != 0)
            {
                return addDriverResult.ExitCode;
            }

            var selectedDriver = await GetSelectedSamsungDownloadDriverAsync(cancellationToken).ConfigureAwait(false);
            HeimdallDebugLogService.Write("Driver", "SelectedDriver=" + selectedDriver);
            if (selectedDriver.IsSamsungCompositeDriver)
            {
                log?.Invoke("检测到 Samsung 原厂复合驱动占用 Download 设备，正在切换到 WinUSB。");
                var deleteResult = await RunProcessAsync("pnputil.exe", "/delete-driver " + selectedDriver.InfName + " /uninstall /force", cancellationToken).ConfigureAwait(false);
                LogCommandResult("pnputil-delete-conflict-driver", deleteResult);
                if (deleteResult.ExitCode != 0)
                {
                    return deleteResult.ExitCode;
                }
            }

            var scanResult = await RunProcessAsync("pnputil.exe", "/scan-devices", cancellationToken).ConfigureAwait(false);
            LogCommandResult("pnputil-scan-devices", scanResult);
            return scanResult.ExitCode;
        }

        /// <summary>
        /// 非管理员进程通过临时脚本请求管理员权限执行驱动修复。
        /// </summary>
        /// <param name="log">任务详情日志回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>提升权限脚本退出码。</returns>
        private async Task<int> RunElevatedRepairScriptAsync(Action<string> log, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_driverPackageDirectory);
            var scriptPath = Path.Combine(_driverPackageDirectory, "repair_samsung_download_winusb.cmd");
            var exitCodePath = Path.Combine(_driverPackageDirectory, "repair_exit_code.txt");
            var scriptLogPath = Path.Combine(_driverPackageDirectory, "repair_driver.log");

            File.WriteAllText(scriptPath, BuildElevatedRepairScript(exitCodePath, scriptLogPath), Encoding.Default);
            if (File.Exists(exitCodePath))
            {
                File.Delete(exitCodePath);
            }

            log?.Invoke("需要管理员权限自动修复 Samsung Download 的 WinUSB 驱动。");
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                process.Start();
                while (!process.HasExited)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(300, cancellationToken).ConfigureAwait(false);
                }
            }

            AppendElevatedScriptLog(scriptLogPath);
            if (int.TryParse(ReadTextIfExists(exitCodePath).Trim(), out var exitCode))
            {
                HeimdallDebugLogService.Write("Driver", "ElevatedRepairExitCode=" + exitCode);
                return exitCode;
            }

            HeimdallDebugLogService.Write("Driver", "ElevatedRepairExitCodeMissing");
            return -1;
        }

        private string BuildElevatedRepairScript(string exitCodePath, string scriptLogPath)
        {
            var builder = new StringBuilder();
            builder.AppendLine("@echo off");
            builder.AppendLine("setlocal");
            builder.AppendLine("set EXIT_CODE=0");
            builder.AppendLine("echo [%date% %time%] start > " + QuoteForCmd(scriptLogPath));
            builder.AppendLine(QuoteForCmd(_driverInstallerPath) + " " + BuildExtractArguments() + " >> " + QuoteForCmd(scriptLogPath) + " 2>&1");
            builder.AppendLine("if errorlevel 1 set EXIT_CODE=%errorlevel%");
            builder.AppendLine("if %EXIT_CODE% EQU 0 pnputil /add-driver " + QuoteForCmd(_exactInfPath) + " /install >> " + QuoteForCmd(scriptLogPath) + " 2>&1");
            builder.AppendLine("if errorlevel 1 if %EXIT_CODE% EQU 0 set EXIT_CODE=%errorlevel%");
            builder.AppendLine("if %EXIT_CODE% EQU 0 powershell -NoProfile -ExecutionPolicy Bypass -Command \"$dev=Get-PnpDevice -PresentOnly | Where-Object { $_.InstanceId -like 'USB\\VID_04E8&PID_685D*' } | Select-Object -First 1; if ($dev) { $service=(Get-PnpDeviceProperty -InstanceId $dev.InstanceId -KeyName 'DEVPKEY_Device_Service' -ErrorAction SilentlyContinue).Data; $inf=(Get-PnpDeviceProperty -InstanceId $dev.InstanceId -KeyName 'DEVPKEY_Device_DriverInfPath' -ErrorAction SilentlyContinue).Data; if ($service -eq 'dg_ssudbus' -and $inf) { pnputil /delete-driver $inf /uninstall /force } }\" >> " + QuoteForCmd(scriptLogPath) + " 2>&1");
            builder.AppendLine("if errorlevel 1 if %EXIT_CODE% EQU 0 set EXIT_CODE=%errorlevel%");
            builder.AppendLine("if %EXIT_CODE% EQU 0 pnputil /scan-devices >> " + QuoteForCmd(scriptLogPath) + " 2>&1");
            builder.AppendLine("if errorlevel 1 if %EXIT_CODE% EQU 0 set EXIT_CODE=%errorlevel%");
            builder.AppendLine("echo %EXIT_CODE% > " + QuoteForCmd(exitCodePath));
            builder.AppendLine("exit /b %EXIT_CODE%");
            return builder.ToString();
        }

        private string BuildExtractArguments()
        {
            return "--name \"Samsung Download\" " +
                   "--manufacturer \"Samsung\" " +
                   "--inf \"" + ExactInfName + "\" " +
                   "--vid " + SamsungVendorId + " " +
                   "--pid " + SamsungDownloadProductId + " " +
                   "--type 0 " +
                   "--dest " + Quote(_driverPackageDirectory) + " " +
                   "--extract " +
                   "--log 0";
        }

        private async Task<SamsungDownloadDriverState> GetSelectedSamsungDownloadDriverAsync(CancellationToken cancellationToken)
        {
            var command = "$dev=Get-PnpDevice -PresentOnly | Where-Object { $_.InstanceId -like 'USB\\VID_04E8&PID_685D*' } | Select-Object -First 1; " +
                          "if ($dev) { " +
                          "$service=(Get-PnpDeviceProperty -InstanceId $dev.InstanceId -KeyName 'DEVPKEY_Device_Service' -ErrorAction SilentlyContinue).Data; " +
                          "$inf=(Get-PnpDeviceProperty -InstanceId $dev.InstanceId -KeyName 'DEVPKEY_Device_DriverInfPath' -ErrorAction SilentlyContinue).Data; " +
                          "$name=$dev.FriendlyName; " +
                          "Write-Output ($dev.InstanceId + '|jx|' + $name + '|jx|' + $service + '|jx|' + $inf) " +
                          "}";
            var result = await RunPowerShellAsync(command, cancellationToken).ConfigureAwait(false);
            LogCommandResult("query-selected-driver", result);
            return SamsungDownloadDriverState.Parse(result.StandardOutput);
        }

        private async Task LogCurrentDeviceStateAsync(string stage, CancellationToken cancellationToken)
        {
            var command = "Get-PnpDevice -PresentOnly | Where-Object { $_.InstanceId -like 'USB\\VID_04E8*' } | " +
                          "ForEach-Object { $service=(Get-PnpDeviceProperty -InstanceId $_.InstanceId -KeyName 'DEVPKEY_Device_Service' -ErrorAction SilentlyContinue).Data; " +
                          "$inf=(Get-PnpDeviceProperty -InstanceId $_.InstanceId -KeyName 'DEVPKEY_Device_DriverInfPath' -ErrorAction SilentlyContinue).Data; " +
                          "Write-Output ($_.FriendlyName + '|jx|' + $_.InstanceId + '|jx|' + $_.Class + '|jx|' + $service + '|jx|' + $inf) }";
            var result = await RunPowerShellAsync(command, cancellationToken).ConfigureAwait(false);
            HeimdallDebugLogService.Write("Driver", "DeviceState-" + stage + "=" + result.StandardOutput.Trim());
            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                HeimdallDebugLogService.Write("Driver", "DeviceState-" + stage + "-Error=" + result.StandardError.Trim());
            }
        }

        private static async Task<CommandResult> RunPowerShellAsync(string command, CancellationToken cancellationToken)
        {
            return await RunProcessAsync(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command " + Quote(command),
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<CommandResult> RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
        {
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.Default,
                    StandardErrorEncoding = Encoding.Default
                };

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                };

                HeimdallDebugLogService.Write("Driver", fileName + " " + arguments);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                while (!process.HasExited)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                }

                process.WaitForExit();
                return new CommandResult(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
            }
        }

        private static void LogCommandResult(string stage, CommandResult result)
        {
            HeimdallDebugLogService.Write("Driver", stage + " ExitCode=" + result.ExitCode);
            HeimdallDebugLogService.Write("Driver", stage + " StdOut=" + result.StandardOutput.Trim());
            HeimdallDebugLogService.Write("Driver", stage + " StdErr=" + result.StandardError.Trim());
        }

        private static void AppendElevatedScriptLog(string scriptLogPath)
        {
            var content = ReadTextIfExists(scriptLogPath);
            if (!string.IsNullOrWhiteSpace(content))
            {
                HeimdallDebugLogService.Write("Driver", "ElevatedScriptLog=" + content);
            }
        }

        private static string ReadTextIfExists(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path, Encoding.Default) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string QuoteForCmd(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }

        private static string ResolveDriverInstallerPath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, "Library", "Heimdall", "wdi-64bit.exe");
        }

        private sealed class CommandResult
        {
            public CommandResult(int exitCode, string standardOutput, string standardError)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput ?? string.Empty;
                StandardError = standardError ?? string.Empty;
            }

            public int ExitCode { get; }

            public string StandardOutput { get; }

            public string StandardError { get; }
        }

        private sealed class SamsungDownloadDriverState
        {
            public string InstanceId { get; private set; }

            public string FriendlyName { get; private set; }

            public string ServiceName { get; private set; }

            public string InfName { get; private set; }

            public bool IsSamsungCompositeDriver =>
                InstanceId != null &&
                InstanceId.StartsWith(SamsungDownloadHardwareId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(ServiceName, "dg_ssudbus", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(InfName) &&
                InfName.StartsWith("oem", StringComparison.OrdinalIgnoreCase);

            public static SamsungDownloadDriverState Parse(string output)
            {
                var line = (output ?? string.Empty).Trim();
                var parts = line.Split(new[] { "|jx|" }, StringSplitOptions.None);
                return new SamsungDownloadDriverState
                {
                    InstanceId = parts.Length > 0 ? parts[0] : string.Empty,
                    FriendlyName = parts.Length > 1 ? parts[1] : string.Empty,
                    ServiceName = parts.Length > 2 ? parts[2] : string.Empty,
                    InfName = parts.Length > 3 ? parts[3] : string.Empty
                };
            }

            public override string ToString()
            {
                return "InstanceId=" + InstanceId +
                       ", FriendlyName=" + FriendlyName +
                       ", ServiceName=" + ServiceName +
                       ", InfName=" + InfName;
            }
        }
    }
}
