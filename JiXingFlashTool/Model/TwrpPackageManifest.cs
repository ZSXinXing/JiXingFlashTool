using System.Collections.Generic;
using System.Runtime.Serialization;

namespace JiXingFlashTool.Model
{
    /// <summary>
    /// TWRP 压缩包中 data.json 的清单模型。
    /// </summary>
    [DataContract]
    internal sealed class TwrpPackageManifest
    {
        /// <summary>
        /// TWRP Recovery 镜像条目。
        /// </summary>
        [DataMember(Name = "recoveryImages")]
        public List<TwrpRecoveryImage> RecoveryImages { get; set; }
    }
}
