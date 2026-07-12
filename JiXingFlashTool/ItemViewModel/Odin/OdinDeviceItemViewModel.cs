using CommunityToolkit.Mvvm.ComponentModel;
using JXHeimdall.Models;
using System.IO;
using System.Linq;

namespace JiXingFlashTool.ItemViewModel.Odin
{
    /// <summary>
    /// Odin 刷机列表中的单台 Download 设备项，负责承载设备状态、固件分配和进度展示。
    /// </summary>
    public sealed class OdinDeviceItemViewModel : ObservableObject
    {
        private bool _isSelected;
        private string _blFilePath = string.Empty;
        private string _apFilePath = string.Empty;
        private string _twrpFilePath = string.Empty;
        private string _systemPackageFilePath = string.Empty;
        private bool _wipeDataBeforeSystemFlash;
        private bool _wipeSystemBeforeSystemFlash;
        private bool _formatDataBeforeSystemFlash;
        private string _cpFilePath = string.Empty;
        private string _cscFilePath = string.Empty;
        private string _userdataFilePath = string.Empty;
        private string _statusText = "-";

        /// <summary>
        /// 初始化 Odin 设备项。
        /// </summary>
        /// <param name="device">Download 模式设备模型。</param>
        public OdinDeviceItemViewModel(HeimdallDeviceModel device)
        {
            Device = device;
        }

        /// <summary>
        /// Download 模式设备模型。
        /// </summary>
        public HeimdallDeviceModel Device { get; }

        /// <summary>
        /// 列表序号。
        /// </summary>
        public int DisplayIndex => Device.DisplayIndex;

        /// <summary>
        /// 设备序列号或 USB 实例标识摘要。
        /// </summary>
        public string Serial => Device.DisplayName;

        /// <summary>
        /// 设备品牌。
        /// </summary>
        public string Brand => "Samsung";

        /// <summary>
        /// 设备型号。
        /// </summary>
        public string ModelName => Device.ModelName;

        /// <summary>
        /// 当前行操作列显示文本，设计稿初始状态为空占位。
        /// </summary>
        public string OperationText => string.Empty;

        /// <summary>
        /// 当前设备是否被选中参与固件分配或刷入。
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 已分配 BL 固件路径。
        /// </summary>
        public string BlFilePath
        {
            get => _blFilePath;
            set => SetFirmwarePath(ref _blFilePath, value, nameof(BlFilePath), nameof(BlFileName));
        }

        /// <summary>
        /// 已分配 AP 固件路径。
        /// </summary>
        public string ApFilePath
        {
            get => _apFilePath;
            set => SetFirmwarePath(ref _apFilePath, value, nameof(ApFilePath), nameof(ApFileName));
        }

        /// <summary>
        /// 用于替换 AP 内置 Recovery 的 TWRP 镜像路径。
        /// </summary>
        public string TwrpFilePath
        {
            get => _twrpFilePath;
            set => SetFirmwarePath(ref _twrpFilePath, value, nameof(TwrpFilePath), nameof(TwrpFileName));
        }

        /// <summary>
        /// 已分配的 TWRP 侧载系统包路径。
        /// </summary>
        public string SystemPackageFilePath
        {
            get => _systemPackageFilePath;
            set => SetFirmwarePath(ref _systemPackageFilePath, value, nameof(SystemPackageFilePath), nameof(SystemPackageFileName));
        }

        /// <summary>
        /// 刷入系统包前是否执行双清。
        /// </summary>
        public bool WipeDataBeforeSystemFlash
        {
            get => _wipeDataBeforeSystemFlash;
            set => SetCleanupOption(ref _wipeDataBeforeSystemFlash, value, nameof(WipeDataBeforeSystemFlash));
        }

        /// <summary>
        /// 刷入系统包前是否清除 system 分区。
        /// </summary>
        public bool WipeSystemBeforeSystemFlash
        {
            get => _wipeSystemBeforeSystemFlash;
            set => SetCleanupOption(ref _wipeSystemBeforeSystemFlash, value, nameof(WipeSystemBeforeSystemFlash));
        }

        /// <summary>
        /// 刷入系统包前是否格式化 data 分区。
        /// </summary>
        public bool FormatDataBeforeSystemFlash
        {
            get => _formatDataBeforeSystemFlash;
            set => SetCleanupOption(ref _formatDataBeforeSystemFlash, value, nameof(FormatDataBeforeSystemFlash));
        }

        /// <summary>
        /// 已分配 CP 固件路径。
        /// </summary>
        public string CpFilePath
        {
            get => _cpFilePath;
            set => SetFirmwarePath(ref _cpFilePath, value, nameof(CpFilePath), nameof(CpFileName));
        }

        /// <summary>
        /// 已分配 CSC 固件路径。
        /// </summary>
        public string CscFilePath
        {
            get => _cscFilePath;
            set => SetFirmwarePath(ref _cscFilePath, value, nameof(CscFilePath), nameof(CscFileName));
        }

        /// <summary>
        /// 已分配 USERDATA 固件路径。
        /// </summary>
        public string UserdataFilePath
        {
            get => _userdataFilePath;
            set => SetFirmwarePath(ref _userdataFilePath, value, nameof(UserdataFilePath), nameof(UserdataFileName));
        }

        /// <summary>
        /// 当前状态和进度展示文本。
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>
        /// BL 文件名，未选择时显示占位横线。
        /// </summary>
        public string BlFileName => GetDisplayFileName(BlFilePath);

        /// <summary>
        /// AP 文件名，未选择时显示占位横线。
        /// </summary>
        public string ApFileName => GetDisplayFileName(ApFilePath);

        /// <summary>
        /// TWRP 镜像文件名，未选择时显示占位横线。
        /// </summary>
        public string TwrpFileName => GetDisplayFileName(TwrpFilePath);

        /// <summary>
        /// TWRP 侧载系统包文件名，未选择时显示占位横线。
        /// </summary>
        public string SystemPackageFileName => GetDisplayFileName(SystemPackageFilePath);

        /// <summary>
        /// CP 文件名，未选择时显示占位横线。
        /// </summary>
        public string CpFileName => GetDisplayFileName(CpFilePath);

        /// <summary>
        /// CSC 文件名，未选择时显示占位横线。
        /// </summary>
        public string CscFileName => GetDisplayFileName(CscFilePath);

        /// <summary>
        /// USERDATA 文件名，未选择时显示占位横线。
        /// </summary>
        public string UserdataFileName => GetDisplayFileName(UserdataFilePath);

        /// <summary>
        /// TWRP 系统安装前的清理选项展示文本，未启用时显示占位横线。
        /// </summary>
        public string CleanupOptionsText
        {
            get
            {
                var options = new[]
                {
                    WipeDataBeforeSystemFlash ? "双清" : string.Empty,
                    WipeSystemBeforeSystemFlash ? "清除系统" : string.Empty,
                    FormatDataBeforeSystemFlash ? "格式化" : string.Empty
                };

                var selectedOptions = string.Join(" | ", options.Where(item => !string.IsNullOrWhiteSpace(item)));
                return string.IsNullOrWhiteSpace(selectedOptions) ? "-" : selectedOptions;
            }
        }

        /// <summary>
        /// 为当前设备分配固件路径。
        /// </summary>
        /// <param name="blFilePath">BL 文件路径。</param>
        /// <param name="apFilePath">AP 文件路径。</param>
        /// <param name="twrpFilePath">TWRP 镜像路径。</param>
        /// <param name="systemPackageFilePath">TWRP 侧载系统包路径。</param>
        /// <param name="wipeDataBeforeSystemFlash">是否在刷系统前双清。</param>
        /// <param name="wipeSystemBeforeSystemFlash">是否在刷系统前清除 system。</param>
        /// <param name="formatDataBeforeSystemFlash">是否在刷系统前格式化 data。</param>
        /// <param name="cpFilePath">CP 文件路径。</param>
        /// <param name="cscFilePath">CSC 文件路径。</param>
        /// <param name="userdataFilePath">USERDATA 文件路径。</param>
        public void AssignFirmware(
            string blFilePath,
            string apFilePath,
            string twrpFilePath,
            string systemPackageFilePath,
            bool wipeDataBeforeSystemFlash,
            bool wipeSystemBeforeSystemFlash,
            bool formatDataBeforeSystemFlash,
            string cpFilePath,
            string cscFilePath,
            string userdataFilePath)
        {
            BlFilePath = blFilePath;
            ApFilePath = apFilePath;
            TwrpFilePath = twrpFilePath;
            SystemPackageFilePath = systemPackageFilePath;
            WipeDataBeforeSystemFlash = wipeDataBeforeSystemFlash;
            WipeSystemBeforeSystemFlash = wipeSystemBeforeSystemFlash;
            FormatDataBeforeSystemFlash = formatDataBeforeSystemFlash;
            CpFilePath = cpFilePath;
            CscFilePath = cscFilePath;
            UserdataFilePath = userdataFilePath;
            StatusText = "已分配";
        }

        /// <summary>
        /// 设置固件路径并同步刷新对应文件名展示，减少各槽位属性的重复通知逻辑。
        /// </summary>
        /// <param name="field">固件路径字段引用。</param>
        /// <param name="value">新的固件路径。</param>
        /// <param name="propertyName">路径属性名称。</param>
        /// <param name="fileNamePropertyName">文件名展示属性名称。</param>
        private void SetFirmwarePath(ref string field, string value, string propertyName, string fileNamePropertyName)
        {
            if (SetProperty(ref field, value, propertyName))
            {
                OnPropertyChanged(fileNamePropertyName);
            }
        }

        /// <summary>
        /// 设置系统安装清理选项并同步刷新列表展示文本。
        /// </summary>
        /// <param name="field">清理选项字段引用。</param>
        /// <param name="value">新的选中状态。</param>
        /// <param name="propertyName">清理选项属性名称。</param>
        private void SetCleanupOption(ref bool field, bool value, string propertyName)
        {
            if (SetProperty(ref field, value, propertyName))
            {
                OnPropertyChanged(nameof(CleanupOptionsText));
            }
        }

        /// <summary>
        /// 获取固件文件展示名称，空路径返回设计稿占位横线。
        /// </summary>
        /// <param name="filePath">固件文件路径。</param>
        /// <returns>文件名或占位横线。</returns>
        private static string GetDisplayFileName(string filePath)
        {
            return string.IsNullOrWhiteSpace(filePath) ? "-" : Path.GetFileName(filePath);
        }
    }
}
