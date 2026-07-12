using System.Collections.Generic;
using System.Runtime.Serialization;

namespace JiXingFlashTool.Model
{
    /// <summary>
    /// TWRP 压缩包中的单个 Recovery 镜像条目模型。
    /// </summary>
    [DataContract]
    internal sealed class TwrpRecoveryImage
    {
        /// <summary>
        /// 压缩包内 TWRP 镜像文件名。
        /// </summary>
        [DataMember(Name = "imageFile")]
        public string ImageFile { get; set; }

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
