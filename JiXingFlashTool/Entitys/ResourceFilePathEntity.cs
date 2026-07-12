using SQLite;

namespace JiXingFlashTool.Entitys
{
    /// <summary>
    /// 资源文件路径的 SQLite 持久化实体。
    /// </summary>
    [Table("resource_file_paths")]
    public sealed class ResourceFilePathEntity
    {
        /// <summary>
        /// 资源唯一标识，格式为资源类型与设备系列组合。
        /// </summary>
        [PrimaryKey]
        public string ResourceKey { get; set; }

        /// <summary>
        /// 资源类型，区分系统固件与 TWRP。
        /// </summary>
        [Indexed]
        public string ResourceType { get; set; }

        /// <summary>
        /// 设备系列标识，例如 S7、S8、S9。
        /// </summary>
        [Indexed]
        public string Series { get; set; }

        /// <summary>
        /// 资源压缩包的绝对路径。
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 最后保存时间的 Unix 时间戳。
        /// </summary>
        public long UpdatedAtUnix { get; set; }
    }
}
