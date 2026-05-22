using JXHeimdall.Models;
using System;
using System.Diagnostics;
using System.IO;
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
                throw new FileNotFoundException("未找到 heimdall.exe，请放置到 Library\\Heimdall\\heimdall.exe 或配置系统 PATH。", _heimdallExecutablePath);
            }

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var stopwatch = Stopwatch.StartNew();

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = _heimdallExecutablePath,
                    Arguments = arguments,
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

                    outputBuilder.AppendLine(e.Data);
                    log?.Invoke(e.Data);
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data == null)
                    {
                        return;
                    }

                    errorBuilder.AppendLine(e.Data);
                    log?.Invoke(e.Data);
                };

                log?.Invoke("heimdall " + arguments);
                HeimdallDebugLogService.Write("Process", "heimdall " + arguments);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                while (!process.HasExited)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                }

                stopwatch.Stop();
                var result = new HeimdallProcessResult
                {
                    ExitCode = process.ExitCode,
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

            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static string ResolveHeimdallExecutablePath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var bundledPath = Path.Combine(baseDirectory, "Library", "Heimdall", "heimdall.exe");
            if (File.Exists(bundledPath))
            {
                return bundledPath;
            }

            var localPath = Path.Combine(baseDirectory, "heimdall.exe");
            if (File.Exists(localPath))
            {
                return localPath;
            }

            var pathEnvironment = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var path in pathEnvironment.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var candidatePath = Path.Combine(path.Trim(), "heimdall.exe");
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            return bundledPath;
        }
    }
}
