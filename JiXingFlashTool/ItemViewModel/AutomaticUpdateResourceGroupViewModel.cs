using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JiXingFlashTool.ItemViewModel
{
    /// <summary>
    /// 自动更新资源分组项，负责按设备代号汇总设备数量及匹配到的 ROM、TWRP 文件。
    /// </summary>
    public sealed class AutomaticUpdateResourceGroupViewModel : ObservableObject
    {
        private readonly IReadOnlyList<AutomaticUpdateDeviceItemViewModel> _devices;
        private string _romFileName = string.Empty;
        private string _twrpFileName = string.Empty;

        /// <summary>
        /// 使用设备代号和对应设备初始化资源分组。
        /// </summary>
        /// <param name="seriesName">设备代号。</param>
        /// <param name="devices">属于该代号的设备项。</param>
        /// <param name="deviceCountText">本地化后的设备数量文案。</param>
        public AutomaticUpdateResourceGroupViewModel(
            string seriesName,
            IReadOnlyList<AutomaticUpdateDeviceItemViewModel> devices,
            string deviceCountText)
        {
            SeriesName = seriesName;
            _devices = devices;
            DeviceCountText = deviceCountText;
        }

        /// <summary>
        /// 当前资源分组对应的设备代号。
        /// </summary>
        public string SeriesName { get; }

        /// <summary>
        /// 当前设备代号包含的已选设备数量文案。
        /// </summary>
        public string DeviceCountText { get; }

        /// <summary>
        /// 当前设备代号匹配到的 ROM 文件名。
        /// </summary>
        public string RomFileName
        {
            get => _romFileName;
            private set => SetProperty(ref _romFileName, value);
        }

        /// <summary>
        /// 当前设备代号匹配到的 TWRP 文件名。
        /// </summary>
        public string TwrpFileName
        {
            get => _twrpFileName;
            private set => SetProperty(ref _twrpFileName, value);
        }

        /// <summary>
        /// 根据设备自动识别结果刷新当前设备代号展示文件名。
        /// </summary>
        /// <param name="showRom">是否显示 ROM 文件。</param>
        /// <param name="showTwrp">是否显示 TWRP 文件。</param>
        public void RefreshFileNames(bool showRom, bool showTwrp)
        {
            AutomaticUpdateDeviceItemViewModel resolvedDevice = _devices.FirstOrDefault(device => device.IsResolved);
            RomFileName = showRom ? GetFileName(resolvedDevice?.RomFilePath) : string.Empty;
            TwrpFileName = showTwrp ? GetFileName(resolvedDevice?.TwrpFilePath) : string.Empty;
        }

        /// <summary>
        /// 从完整路径中取得用于界面展示的文件名。
        /// </summary>
        /// <param name="filePath">本地文件路径。</param>
        /// <returns>路径为空时返回空字符串。</returns>
        private static string GetFileName(string filePath)
        {
            return string.IsNullOrWhiteSpace(filePath) ? string.Empty : Path.GetFileName(filePath);
        }
    }
}
