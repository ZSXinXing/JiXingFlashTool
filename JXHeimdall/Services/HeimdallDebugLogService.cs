using System;
using System.IO;
using System.Text;

namespace JXHeimdall.Services
{
    /// <summary>
    /// Heimdall 模块调试日志服务，负责把驱动安装和进程诊断写入 debug_log。
    /// </summary>
    internal static class HeimdallDebugLogService
    {
        private static readonly object SyncRoot = new object();

        /// <summary>
        /// 获取 Heimdall 调试日志文件路径。
        /// </summary>
        public static string CurrentLogPath
        {
            get
            {
                var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log");
                return Path.Combine(logDirectory, "heimdall_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            }
        }

        /// <summary>
        /// 写入一条 Heimdall 调试日志。
        /// </summary>
        /// <param name="module">模块名称。</param>
        /// <param name="message">日志内容。</param>
        public static void Write(string module, string message)
        {
            try
            {
                var logPath = CurrentLogPath;
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                           " [" + module + "] " +
                           (message ?? string.Empty) +
                           Environment.NewLine;

                lock (SyncRoot)
                {
                    File.AppendAllText(logPath, line, Encoding.UTF8);
                }
            }
            catch
            {
                // 调试日志不能影响刷机主流程。
            }
        }
    }
}
