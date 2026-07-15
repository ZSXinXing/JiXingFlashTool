using System.IO;

namespace JiXingFlashTool.Model
{
    /// <summary>
    /// Odin TWRP 自动选择下拉项，表示资源压缩包中的一个 TWRP 镜像。
    /// </summary>
    public sealed class TwrpResourceOptionModel
    {
        /// <summary>
        /// 资源所属设备系列，例如 S7、S8、S9。
        /// </summary>
        public string Series { get; set; }

        /// <summary>
        /// TWRP 镜像对应的主板代号。
        /// </summary>
        public string Board { get; set; }

        /// <summary>
        /// TWRP 镜像编译日期。
        /// </summary>
        public string BuildDate { get; set; }

        /// <summary>
        /// TWRP 镜像在资源压缩包内的文件名。
        /// </summary>
        public string ImageFile { get; set; }

        /// <summary>
        /// TWRP 资源压缩包的本地绝对路径。
        /// </summary>
        public string ArchivePath { get; set; }

        /// <summary>
        /// 下拉框中展示的资源名称。
        /// </summary>
        public string DisplayName
        {
            get
            {
                string fileName = Path.GetFileName(ImageFile);
                return $"{Series} {Board} {BuildDate} {fileName}".Trim();
            }
        }
    }
}
