using JXHeimdall.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXHeimdall.Services
{
    /// <summary>
    /// 负责查找并执行 heimdall.exe，集中处理进程输出、超时和取消。
    /// </summary>
    public sealed class HeimdallProcessService
    {
        // 刷入过程中持续输出进度，超过该时间无输出即可判定进程或 USB 通信异常。
        private static readonly TimeSpan ProcessInactivityTimeout = TimeSpan.FromMinutes(3);
        private const string KnownLargeFileIssueHash = "4DDAE318A50509A1C5ED8F23DC2FADDA2A7A4EF913B0CAC47FEF1B27843DB708";
        private readonly string _heimdallExecutablePath;

        /// <summary>
        /// 使用指定 heimdall.exe 路径初始化进程服务，未指定时自动从程序目录和 PATH 查找。
        /// </summary>
        /// <param name="heimdallExecutablePath">heimdall.exe 路径。</param>
        public HeimdallProcessService(string heimdallExecutablePath = null)
        {
            _heimdallExecutablePath = string.IsNullOrWhiteSpace(heimdallExecutablePath)
                ? ResolveHeimdallExecutablePath()
                : heimdallExecutablePath;
        }

        /// <summary>
        /// 判断当前环境是否能找到 heimdall.exe。
        /// </summary>
        public bool IsAvailable => File.Exists(_heimdallExecutablePath);

        /// <summary>
        /// 当前使用的 heimdall.exe 完整路径。
        /// </summary>
        public string HeimdallExecutablePath => _heimdallExecutablePath;

        /// <summary>
        /// 判断当前 Heimdall 是否为已知无法稳定刷入 2GB 以上 SYSTEM 镜像的旧构建。
        /// </summary>
        public bool HasKnownLargeSystemImageIssue
        {
            get
            {
                return IsKnownLargeSystemImageIssueBuild(_heimdallExecutablePath);
            }
        }

        /// <summary>
        /// 异步执行 Heimdall 参数并返回进程结果。
        /// </summary>
        /// <param name="arguments">命令行参数。</param>
        /// <param name="log">日志回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>进程执行结果。</returns>
        public async Task<HeimdallProcessResult> ExecuteAsync(string arguments, Action<string> log, CancellationToken cancellationToken)
        {
            if (!IsAvailable)
            {
                throw new FileNotFoundException("未找到 heimdall.exe，请放置到 Resources\\Library\\Heimdall\\heimdall.exe 或配置系统 PATH。", _heimdallExecutablePath);
            }

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var stopwatch = Stopwatch.StartNew();
            var lastOutputTicks = DateTime.UtcNow.Ticks;

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = _heimdallExecutablePath,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetDirectoryName(_heimdallExecutablePath),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data == null)
                    {
                        return;
                    }

                    Interlocked.Exchange(ref lastOutputTicks, DateTime.UtcNow.Ticks);
                    outputBuilder.AppendLine(e.Data);
                    log?.Invoke(e.Data);
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data == null)
                    {
                        return;
                    }

                    Interlocked.Exchange(ref lastOutputTicks, DateTime.UtcNow.Ticks);
                    errorBuilder.AppendLine(e.Data);
                    log?.Invoke(e.Data);
                };

                log?.Invoke("heimdall " + arguments);
                HeimdallDebugLogService.Write("Process", "executable=" + _heimdallExecutablePath);
                HeimdallDebugLogService.Write("Process", "executable-sha256=" + GetFileSha256(_heimdallExecutablePath));
                HeimdallDebugLogService.Write(
                    "Process",
                    "working-directory=" + process.StartInfo.WorkingDirectory +
                    " redirect-output=" + process.StartInfo.RedirectStandardOutput +
                    " redirect-error=" + process.StartInfo.RedirectStandardError +
                    " shell-execute=" + process.StartInfo.UseShellExecute);
                HeimdallDebugLogService.Write("Process", "heimdall " + arguments);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var isTimedOut = false;
                var timeoutMessage = string.Empty;
                try
                {
                    while (!process.HasExited)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var lastOutputTime = new DateTime(Interlocked.Read(ref lastOutputTicks), DateTimeKind.Utc);
                        if (DateTime.UtcNow - lastOutputTime >= ProcessInactivityTimeout)
                        {
                            isTimedOut = true;
                            break;
                        }

                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    }

                    if (isTimedOut)
                    {
                        timeoutMessage = "Heimdall 连续 3 分钟没有任何输出，已终止异常进程。";
                        log?.Invoke(timeoutMessage);
                        HeimdallDebugLogService.Write("Process", "inactivity-timeout arguments=" + arguments);
                        TryTerminateProcess(process);
                    }

                    process.WaitForExit();
                    if (isTimedOut)
                    {
                        errorBuilder.AppendLine(timeoutMessage);
                    }
                }
                catch (OperationCanceledException)
                {
                    TryTerminateProcess(process);
                    throw;
                }

                stopwatch.Stop();
                var result = new HeimdallProcessResult
                {
                    ExitCode = isTimedOut ? -1 : process.ExitCode,
                    StandardOutput = outputBuilder.ToString(),
                    StandardError = errorBuilder.ToString(),
                    Arguments = arguments,
                    Elapsed = stopwatch.Elapsed
                };
                HeimdallDebugLogService.Write(
                    "Process",
                    "exit-code=" + result.ExitCode +
                    " elapsed=" + result.Elapsed +
                    " stdout=" + result.StandardOutput.Trim() +
                    " stderr=" + result.StandardError.Trim());

                return result;
            }
        }

        /// <summary>
        /// 在任务取消或进程无输出超时时结束 Heimdall，避免后台残留继续占用 USB 设备。
        /// </summary>
        /// <param name="process">需要结束的 Heimdall 进程。</param>
        private static void TryTerminateProcess(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                HeimdallDebugLogService.Write("Process", "terminate-failed " + ex.Message);
            }
        }

        /// <summary>
        /// 为命令行参数添加安全引号。
        /// </summary>
        /// <param name="value">原始参数值。</param>
        /// <returns>已转义的参数值。</returns>
        public static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// 获取项目输出目录中内置的 Heimdall 可执行文件。
        /// </summary>
        /// <returns>可执行的 heimdall.exe 完整路径。</returns>
        private static string ResolveHeimdallExecutablePath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, "Resources", "Library", "Heimdall", "heimdall.exe");
        }

        /// <summary>
        /// 判断指定 Heimdall 文件是否为已知 2GB 大文件刷入异常构建。
        /// </summary>
        /// <param name="filePath">Heimdall 可执行文件路径。</param>
        /// <returns>属于已知异常构建时返回 true。</returns>
        private static bool IsKnownLargeSystemImageIssueBuild(string filePath)
        {
            return string.Equals(
                GetFileSha256(filePath),
                KnownLargeFileIssueHash,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 计算文件 SHA256，用于区分 Heimdall 修复版与旧异常构建。
        /// </summary>
        /// <param name="filePath">文件路径。</param>
        /// <returns>文件哈希；文件不存在或计算失败时返回空字符串。</returns>
        private static string GetFileSha256(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return string.Empty;
                }

                using (var stream = File.OpenRead(filePath))
                using (var sha256 = SHA256.Create())
                {
                    return BitConverter
                        .ToString(sha256.ComputeHash(stream))
                        .Replace("-", string.Empty);
                }
            }
            catch (Exception ex)
            {
                HeimdallDebugLogService.Write("Process", "hash-failed " + ex.Message);
                return string.Empty;
            }
        }
    }
}
