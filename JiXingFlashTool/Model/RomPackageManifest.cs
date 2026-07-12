using System.Collections.Generic;
using System.Runtime.Serialization;

namespace JiXingFlashTool.Model
{
    /// <summary>
    /// ROM 压缩包中 data.json 的清单模型。
    /// </summary>
    [DataContract]
    internal sealed class RomPackageManifest
    {
        /// <summary>
        /// ROM 固件文件条目。
        /// </summary>
        [DataMember(Name = "firmwarePackages")]
        public List<RomFirmwarePackage> FirmwarePackages { get; set; }
    }
}
