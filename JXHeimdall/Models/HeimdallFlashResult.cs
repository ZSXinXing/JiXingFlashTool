namespace JXHeimdall.Models
{
    /// <summary>
    /// 表示一次 Heimdall 刷入流程的业务结果。
    /// </summary>
    public sealed class HeimdallFlashResult
    {
        /// <summary>
        /// 刷入流程是否成功完成。
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 用户或日志可读的结果信息。
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 最后一次 Heimdall 进程调用结果。
        /// </summary>
        public HeimdallProcessResult LastProcessResult { get; set; }
    }
}
