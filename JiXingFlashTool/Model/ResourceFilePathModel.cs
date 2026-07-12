using System;

namespace JiXingFlashTool.Model
{
    /// <summary>
    /// 资源管理功能使用的单个 ROM 或 TWRP 文件路径。
    /// </summary>
    public sealed class ResourceFilePathModel
    {
        /// <summary>
        /// 资源类型，区分系统固件与 TWRP。
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// 设备系列标识，例如 S7、S8、S9。
        /// </summary>
        public string Series { get; set; }

        /// <summary>
        /// 资源压缩包的绝对路径。
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 最后保存时间。
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
