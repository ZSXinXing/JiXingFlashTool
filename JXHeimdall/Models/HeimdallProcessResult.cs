using System;

namespace JXHeimdall.Models
{
    /// <summary>
    /// 表示一次 heimdall.exe 进程调用结果。
    /// </summary>
    public sealed class HeimdallProcessResult
    {
        /// <summary>
        /// 进程退出码。
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// 标准输出内容。
        /// </summary>
        public string StandardOutput { get; set; } = string.Empty;

        /// <summary>
        /// 标准错误内容。
        /// </summary>
        public string StandardError { get; set; } = string.Empty;

        /// <summary>
        /// 实际执行的命令行参数。
        /// </summary>
        public string Arguments { get; set; } = string.Empty;

        /// <summary>
        /// 进程执行耗时。
        /// </summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// 当前调用是否成功退出。
        /// </summary>
        public bool IsSuccess => ExitCode == 0;
    }
}
