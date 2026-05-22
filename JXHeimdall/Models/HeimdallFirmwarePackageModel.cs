using System.Collections.Generic;

namespace JXHeimdall.Models
{
    /// <summary>
    /// 表示已解析的 Odin 固件包及其可刷入文件集合。
    /// </summary>
    public sealed class HeimdallFirmwarePackageModel
    {
        /// <summary>
        /// 固件槽位。
        /// </summary>
        public HeimdallFirmwareSlot Slot { get; set; }

        /// <summary>
        /// 原始固件包路径。
        /// </summary>
        public string SourceFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 固件解包临时目录。
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 可参与刷入的固件文件集合。
        /// </summary>
        public List<HeimdallFirmwareFileModel> Files { get; } = new List<HeimdallFirmwareFileModel>();
    }
}
