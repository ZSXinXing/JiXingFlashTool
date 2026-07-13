using CommunityToolkit.Mvvm.ComponentModel;
using JiXingFlashTool.Model;

namespace JiXingFlashTool.ItemViewModel
{
    /// <summary>
    /// 自动更新列表中的单个设备项，负责展示设备代号及 ROM、TWRP 自动匹配结果。
    /// </summary>
    public sealed class AutomaticUpdateDeviceItemViewModel : ObservableObject
    {
        private string _romVersion = string.Empty;
        private string _twrpVersion = string.Empty;
        private string _statusText;
        private string _errorMessage = string.Empty;
        private string _romFilePath = string.Empty;
        private string _twrpFilePath = string.Empty;

        /// <summary>
        /// 使用选中的设备项初始化自动识别行。
        /// </summary>
        /// <param name="deviceItem">当前选中的设备项。</param>
        /// <param name="waitingText">等待识别状态文案。</param>
        public AutomaticUpdateDeviceItemViewModel(DeviceItemViewModel deviceItem, string waitingText)
        {
            DeviceItem = deviceItem;
            _statusText = waitingText;
        }

        /// <summary>
        /// 当前行对应的设备项。
        /// </summary>
        public DeviceItemViewModel DeviceItem { get; }

        /// <summary>
        /// 设备界面展示型号。
        /// </summary>
        public string DeviceModel => DeviceItem?.Model ?? string.Empty;

        /// <summary>
        /// 用于匹配资源清单的设备代号。
        /// </summary>
        public string DeviceCode => DeviceItem?.Device?.Name ?? string.Empty;

        /// <summary>
        /// 自动匹配到的 ROM 版本。
        /// </summary>
        public string RomVersion
        {
            get => _romVersion;
            set => SetProperty(ref _romVersion, value);
        }

        /// <summary>
        /// 自动匹配到的 TWRP 版本或编译日期。
        /// </summary>
        public string TwrpVersion
        {
            get => _twrpVersion;
            set => SetProperty(ref _twrpVersion, value);
        }

        /// <summary>
        /// 当前自动识别状态文案。
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>
        /// 自动识别失败的用户可见原因。
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        /// <summary>
        /// 自动提取后的 ROM 文件路径。
        /// </summary>
        public string RomFilePath
        {
            get => _romFilePath;
            set => SetProperty(ref _romFilePath, value);
        }

        /// <summary>
        /// 自动提取后的 TWRP 文件路径。
        /// </summary>
        public string TwrpFilePath
        {
            get => _twrpFilePath;
            set => SetProperty(ref _twrpFilePath, value);
        }

        /// <summary>
        /// 当前行是否存在自动识别错误。
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// 当前设备是否已获得可用的 ROM 文件。
        /// </summary>
        public bool HasRom => !string.IsNullOrWhiteSpace(RomFilePath);

        /// <summary>
        /// 当前设备是否已获得可用的 TWRP 文件。
        /// </summary>
        public bool HasTwrp => !string.IsNullOrWhiteSpace(TwrpFilePath);

        /// <summary>
        /// 当前行是否已获得可用于更新的 ROM 与 TWRP 文件。
        /// </summary>
        public bool IsResolved => HasRom && HasTwrp;
    }
}
