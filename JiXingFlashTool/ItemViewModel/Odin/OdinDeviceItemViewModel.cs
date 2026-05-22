using CommunityToolkit.Mvvm.ComponentModel;
using JXHeimdall.Models;
using System.IO;

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
            set
            {
                if (SetProperty(ref _blFilePath, value))
                {
                    OnPropertyChanged(nameof(BlFileName));
                }
            }
        }

        /// <summary>
        /// 已分配 AP 固件路径。
        /// </summary>
        public string ApFilePath
        {
            get => _apFilePath;
            set
            {
                if (SetProperty(ref _apFilePath, value))
                {
                    OnPropertyChanged(nameof(ApFileName));
                }
            }
        }

        /// <summary>
        /// 用于替换 AP 内置 Recovery 的 TWRP 镜像路径。
        /// </summary>
        public string TwrpFilePath
        {
            get => _twrpFilePath;
            set
            {
                if (SetProperty(ref _twrpFilePath, value))
                {
                    OnPropertyChanged(nameof(TwrpFileName));
                }
            }
        }

        /// <summary>
        /// 已分配 CP 固件路径。
        /// </summary>
        public string CpFilePath
        {
            get => _cpFilePath;
            set
            {
                if (SetProperty(ref _cpFilePath, value))
                {
                    OnPropertyChanged(nameof(CpFileName));
                }
            }
        }

        /// <summary>
        /// 已分配 CSC 固件路径。
        /// </summary>
        public string CscFilePath
        {
            get => _cscFilePath;
            set
            {
                if (SetProperty(ref _cscFilePath, value))
                {
                    OnPropertyChanged(nameof(CscFileName));
                }
            }
        }

        /// <summary>
        /// 已分配 USERDATA 固件路径。
        /// </summary>
        public string UserdataFilePath
        {
            get => _userdataFilePath;
            set
            {
                if (SetProperty(ref _userdataFilePath, value))
                {
                    OnPropertyChanged(nameof(UserdataFileName));
                }
            }
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
        /// 为当前设备分配固件路径。
        /// </summary>
        /// <param name="blFilePath">BL 文件路径。</param>
        /// <param name="apFilePath">AP 文件路径。</param>
        /// <param name="cpFilePath">CP 文件路径。</param>
        /// <param name="cscFilePath">CSC 文件路径。</param>
        /// <param name="userdataFilePath">USERDATA 文件路径。</param>
        public void AssignFirmware(string blFilePath, string apFilePath, string twrpFilePath, string cpFilePath, string cscFilePath, string userdataFilePath)
        {
            BlFilePath = blFilePath;
            ApFilePath = apFilePath;
            TwrpFilePath = twrpFilePath;
            CpFilePath = cpFilePath;
            CscFilePath = cscFilePath;
            UserdataFilePath = userdataFilePath;
            StatusText = "已分配";
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
