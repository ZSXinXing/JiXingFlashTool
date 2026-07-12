namespace JiXingFlashTool.Model
{
    /// <summary>
    /// 按设备代号提取 ROM 与 TWRP 后的结果。
    /// </summary>
    public sealed class RomPackageExtractionResult
    {
        /// <summary>
        /// 用户请求的设备代号。
        /// </summary>
        public string RequestedBoard { get; internal set; }

        /// <summary>
        /// ROM 实际匹配到的主板代号。
        /// </summary>
        public string RomBoard { get; internal set; }

        /// <summary>
        /// 已提取 ROM 文件的完整路径。
        /// </summary>
        public string RomFilePath { get; internal set; }

        /// <summary>
        /// ROM 包版本。
        /// </summary>
        public string RomPackageVersion { get; internal set; }

        /// <summary>
        /// ROM 编译日期。
        /// </summary>
        public string RomBuildDate { get; internal set; }

        /// <summary>
        /// ROM 提取失败原因。
        /// </summary>
        public string RomError { get; internal set; }

        /// <summary>
        /// TWRP 实际匹配到的主板代号。
        /// </summary>
        public string TwrpBoard { get; internal set; }

        /// <summary>
        /// 已提取 TWRP 文件的完整路径。
        /// </summary>
        public string TwrpFilePath { get; internal set; }

        /// <summary>
        /// TWRP 编译日期。
        /// </summary>
        public string TwrpBuildDate { get; internal set; }

        /// <summary>
        /// TWRP 提取失败原因。
        /// </summary>
        public string TwrpError { get; internal set; }

        /// <summary>
        /// ROM 是否已成功取得。
        /// </summary>
        public bool HasRom => !string.IsNullOrWhiteSpace(RomFilePath);

        /// <summary>
        /// TWRP 是否已成功取得。
        /// </summary>
        public bool HasTwrp => !string.IsNullOrWhiteSpace(TwrpFilePath);
    }
}
