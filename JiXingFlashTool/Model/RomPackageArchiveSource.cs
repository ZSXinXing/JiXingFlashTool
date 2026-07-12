namespace JiXingFlashTool.Model
{
    /// <summary>
    /// 一个设备系列对应的 ROM 与 TWRP 加密压缩包路径。
    /// </summary>
    public sealed class RomPackageArchiveSource
    {
        /// <summary>
        /// 设备系列标识，例如 S7、S8、S9。
        /// </summary>
        public string Series { get; set; }

        /// <summary>
        /// 当前设备系列的 ROM 加密压缩包路径。
        /// </summary>
        public string RomPackagePath { get; set; }

        /// <summary>
        /// 当前设备系列的 TWRP 加密压缩包路径。
        /// </summary>
        public string TwrpPackagePath { get; set; }
    }
}
