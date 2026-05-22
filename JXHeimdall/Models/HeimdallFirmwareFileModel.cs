namespace JXHeimdall.Models
{
    /// <summary>
    /// 表示 Odin 固件包内可刷入的单个文件。
    /// </summary>
    public sealed class HeimdallFirmwareFileModel
    {
        /// <summary>
        /// 固件包内原始文件名。
        /// </summary>
        public string EntryName { get; set; } = string.Empty;

        /// <summary>
        /// 解包到临时目录后的本地文件路径。
        /// </summary>
        public string ExtractedFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小。
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 根据文件名推断出的 Heimdall 分区名称。
        /// </summary>
        public string SuggestedPartitionName { get; set; } = string.Empty;
    }
}
