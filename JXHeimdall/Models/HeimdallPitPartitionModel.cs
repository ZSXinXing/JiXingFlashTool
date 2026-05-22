namespace JXHeimdall.Models
{
    /// <summary>
    /// 表示从 PIT 表中解析出的单个分区信息。
    /// </summary>
    public sealed class HeimdallPitPartitionModel
    {
        /// <summary>
        /// PIT 分区索引。
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Heimdall flash 命令使用的分区名称。
        /// </summary>
        public string PartitionName { get; set; } = string.Empty;

        /// <summary>
        /// 分区文件名。
        /// </summary>
        public string FileName { get; set; } = string.Empty;
    }
}
