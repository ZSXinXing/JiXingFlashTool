namespace JXHeimdall.Models
{
    /// <summary>
    /// 表示从 Samsung PIT 分区表中解析出的 Odin 原始分区条目。
    /// </summary>
    public sealed class OdinPitEntryModel
    {
        /// <summary>
        /// 固件二进制类型，Odin 协议在结束刷入序列时需要传回该值。
        /// </summary>
        public int BinaryType { get; set; }

        /// <summary>
        /// 分区所属设备类型，Odin 协议在结束刷入序列时需要传回该值。
        /// </summary>
        public int DeviceType { get; set; }

        /// <summary>
        /// 分区标识，Odin 协议用它定位实际写入分区。
        /// </summary>
        public int PartitionId { get; set; }

        /// <summary>
        /// 分区属性。
        /// </summary>
        public int Attributes { get; set; }

        /// <summary>
        /// 分区升级属性。
        /// </summary>
        public int UpdateAttributes { get; set; }

        /// <summary>
        /// 分区起始块。
        /// </summary>
        public int BlockSize { get; set; }

        /// <summary>
        /// 分区块数量。
        /// </summary>
        public int BlockCount { get; set; }

        /// <summary>
        /// 旧版 PIT 文件偏移字段。
        /// </summary>
        public int FileOffset { get; set; }

        /// <summary>
        /// 旧版 PIT 文件大小字段。
        /// </summary>
        public int FileSize { get; set; }

        /// <summary>
        /// 分区名称，例如 BOOT、SYSTEM、RADIO。
        /// </summary>
        public string PartitionName { get; set; } = string.Empty;

        /// <summary>
        /// Odin 固件包中对应的刷入文件名。
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 增量升级文件名。
        /// </summary>
        public string DeltaName { get; set; } = string.Empty;
    }
}
