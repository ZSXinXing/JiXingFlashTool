using System.Collections.Generic;
using System.Runtime.Serialization;

namespace JiXingFlashTool.Model
{
    /// <summary>
    /// ROM 压缩包中的单个固件文件条目模型。
    /// </summary>
    [DataContract]
    internal sealed class RomFirmwarePackage
    {
        /// <summary>
        /// 压缩包内 ROM 文件名。
        /// </summary>
        [DataMember(Name = "firmwareFile")]
        public string FirmwareFile { get; set; }

        /// <summary>
        /// ROM 包版本。
        /// </summary>
        [DataMember(Name = "packageVersion")]
        public string PackageVersion { get; set; }

        /// <summary>
        /// 主板代号。
        /// </summary>
        [DataMember(Name = "board")]
        public string Board { get; set; }

        /// <summary>
        /// 兼容主板代号。
        /// </summary>
        [DataMember(Name = "compatibleBoards")]
        public List<string> CompatibleBoards { get; set; }

        /// <summary>
        /// 编译日期。
        /// </summary>
        [DataMember(Name = "buildDate")]
        public string BuildDate { get; set; }
    }
}
