using CommunityToolkit.Mvvm.ComponentModel;
using JiXingFlashTool.Model;
using JiXingFlashTool.Models;
using JiXingFlashTool.Services;
using LanguageCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace JiXingFlashTool.ItemViewModel
{
    /// <summary>
    /// 设备列表中的单个设备展示模型，负责承载主页面表格展示所需的状态与样式数据。
    /// </summary>
    public class DeviceItemViewModel : ObservableObject
    {
        private static readonly SolidColorBrush UsbTagBackgroundBrush = CreateBrush("#E3F2FD");
        private static readonly SolidColorBrush UsbTagForegroundBrush = CreateBrush("#2196F3");
        private static readonly SolidColorBrush EthernetTagBackgroundBrush = CreateBrush("#E0F7FA");
        private static readonly SolidColorBrush EthernetTagForegroundBrush = CreateBrush("#00BCD4");
        private static readonly SolidColorBrush SystemTagBackgroundBrush = CreateBrush("#E8F5E9");
        private static readonly SolidColorBrush SystemTagForegroundBrush = CreateBrush("#4CAF50");
        private static readonly SolidColorBrush RecoveryTagBackgroundBrush = CreateBrush("#FFF3E0");
        private static readonly SolidColorBrush RecoveryTagForegroundBrush = CreateBrush("#FF9800");
        private static readonly SolidColorBrush DownloadTagBackgroundBrush = CreateBrush("#F3E5F5");
        private static readonly SolidColorBrush DownloadTagForegroundBrush = CreateBrush("#9C27B0");
        private static readonly SolidColorBrush SideloadTagBackgroundBrush = CreateBrush("#FCE4EC");
        private static readonly SolidColorBrush SideloadTagForegroundBrush = CreateBrush("#E91E63");
        private static readonly SolidColorBrush OfflineTagBackgroundBrush = CreateBrush("#FFEBEE");
        private static readonly SolidColorBrush OfflineTagForegroundBrush = CreateBrush("#F44336");
        private static readonly SolidColorBrush DefaultTagBackgroundBrush = CreateBrush("#ECEFF1");
        private static readonly SolidColorBrush DefaultTagForegroundBrush = CreateBrush("#607D8B");

        protected DeviceModel device;
        /// <summary>
        /// 当前项对应的设备模型。
        /// </summary>
        public DeviceModel Device { get { return device; } }

        private DeviceTaskService service;
        /// <summary>
        /// 当前设备任务服务，按需创建以降低初始化开销。
        /// </summary>
        public DeviceTaskService Service {
            get {
                if (service == null) service = new DeviceTaskService(this);
                return service;
            }
        }

        private int displayIndex;

        /// <summary>
        /// 列表序号，供主页面表格展示。
        /// </summary>
        public int DisplayIndex
        {
            get => displayIndex;
            set => SetProperty(ref displayIndex, value);
        }

        /// <summary>
        /// 设备序列号，网络连接时仅展示主机地址部分。
        /// </summary>
        public string Serial
        {
            get => Device.Serial;
        }

        /// <summary>
        /// 设备型号。
        /// </summary>
        public string Model
        {
            get => Device.Model;
        }

        /// <summary>
        /// 设备展示名称。
        /// </summary>
        public string ModelName
        {
            get => Device.Name;
        }

        /// <summary>
        /// 设备品牌，对应 ro.product.brand。
        /// </summary>
        public string Brand
        {
            get => Device.Brand;
        }

        /// <summary>
        /// 安卓或 Recovery 版本信息。
        /// </summary>
        public string AndroidVersion
        {
            get {
                if (Device.State == JXAdbCore.Enums.DeviceState.Recovery) return Device.TWRPVersion;
                if(Device.State == JXAdbCore.Enums.DeviceState.Online) return Device.AndroidVersion;
                return "";
            }
        }

        /// <summary>
        /// 系统版本号。
        /// </summary>
        public string SystemVersion {
            get {
                return Device.PolestarVersion;
            }
        }

        /// <summary>
        /// 设备名称。
        /// </summary>
        public string Name
        {
            get
            {
                return Device.Name;
            }
        }

        /// <summary>
        /// 列表中的显示名称。
        /// </summary>
        public string ShowName {
            get => Device.Model;
        }

        /// <summary>
        /// 当前设备状态的本地化文案。
        /// </summary>
        public string DeviceState => GetDeviceStateText(device.State);

        /// <summary>
        /// 当前设备的连接方式文案。
        /// </summary>
        public string ConnectionType => IsEthernetConnection ? GetLangText("Connection_Ethernet") : "USB";

        /// <summary>
        /// 当前设备是否通过以太网连接。
        /// </summary>
        private bool IsEthernetConnection => Device.Serial != null && Device.Serial.Contains(":");

        /// <summary>
        /// 编译日期。
        /// </summary>
        public string BuildDate {
            get {
                return device.BuildDate;
            }
        }

        private bool isSelect;

        /// <summary>
        /// 当前设备是否被选中。
        /// </summary>
        public bool IsSelect
        {
            get => isSelect;
            set
            {
                if (SetProperty(ref isSelect, value))
                {
                    RefreshItemBackgroundColor();
                    RefreshItemForegroundColor();
                    RefreshItemBorderThickness();
                }
            }
        }

        private bool isKeepWhenDisconnected;

        /// <summary>
        /// 断开连接时是否保留该设备项，不立即从列表移除。
        /// </summary>
        public bool IsKeepWhenDisconnected
        {
            get => isKeepWhenDisconnected;
            set => SetProperty(ref isKeepWhenDisconnected, value);
        }

        private string taskDetailMessage;

        /// <summary>
        /// 当前任务详情文案。
        /// </summary>
        public string TaskDetailMessage
        {
            get => taskDetailMessage;
            set
            {
                if (SetProperty(ref taskDetailMessage, value))
                {
                    OnPropertyChanged(nameof(TaskDetailDisplayMessage));
                }
            }
        }

        /// <summary>
        /// 任务详情展示文案，空值时直接保持为空，避免显示默认占位提示。
        /// </summary>
        public string TaskDetailDisplayMessage
        {
            get => string.IsNullOrWhiteSpace(TaskDetailMessage)
                ? string.Empty
                : TaskLogLocalizationService.ResolveMessage(TaskDetailMessage);
        }

        /// <summary>
        /// 连接方式标签背景色。
        /// </summary>
        public SolidColorBrush ConnectionTagBackground
        {
            get => IsEthernetConnection ? EthernetTagBackgroundBrush : UsbTagBackgroundBrush;
        }

        /// <summary>
        /// 连接方式标签前景色。
        /// </summary>
        public SolidColorBrush ConnectionTagForeground
        {
            get => IsEthernetConnection ? EthernetTagForegroundBrush : UsbTagForegroundBrush;
        }

        /// <summary>
        /// 状态标签背景色。
        /// </summary>
        public SolidColorBrush DeviceStateTagBackground
        {
            get
            {
                switch (Device.State)
                {
                    case JXAdbCore.Enums.DeviceState.Online:
                        return SystemTagBackgroundBrush;
                    case JXAdbCore.Enums.DeviceState.Recovery:
                        return RecoveryTagBackgroundBrush;
                    case JXAdbCore.Enums.DeviceState.BootLoader:
                        return DownloadTagBackgroundBrush;
                    case JXAdbCore.Enums.DeviceState.Sideload:
                        return SideloadTagBackgroundBrush;
                    case JXAdbCore.Enums.DeviceState.Offline:
                    case JXAdbCore.Enums.DeviceState.Unauthorized:
                    case JXAdbCore.Enums.DeviceState.NoPermissions:
                        return OfflineTagBackgroundBrush;
                    default:
                        return DefaultTagBackgroundBrush;
                }
            }
        }

        /// <summary>
        /// 状态标签前景色。
        /// </summary>
        public SolidColorBrush DeviceStateTagForeground
        {
            get
            {
                switch (Device.State)
                {
                    case JXAdbCore.Enums.DeviceState.Online:
                        return SystemTagForegroundBrush;
                    case JXAdbCore.Enums.DeviceState.Recovery:
                        return RecoveryTagForegroundBrush;
                    case JXAdbCore.Enums.DeviceState.BootLoader:
                        return DownloadTagForegroundBrush;
                    case JXAdbCore.Enums.DeviceState.Sideload:
                        return SideloadTagForegroundBrush;
                    case JXAdbCore.Enums.DeviceState.Offline:
                    case JXAdbCore.Enums.DeviceState.Unauthorized:
                    case JXAdbCore.Enums.DeviceState.NoPermissions:
                        return OfflineTagForegroundBrush;
                    default:
                        return DefaultTagForegroundBrush;
                }
            }
        }

        /// <summary>
        /// 状态标签圆点颜色。
        /// </summary>
        public SolidColorBrush DeviceStateIndicatorBrush => DeviceStateTagForeground;

        /// <summary>
        /// 是否展示状态圆点。
        /// </summary>
        public bool ShowDeviceStateIndicator => true;

        /// <summary>
        /// 使用设备模型初始化展示模型。
        /// </summary>
        public DeviceItemViewModel(DeviceModel device) => this.device = device;

        /// <summary>
        /// 语言切换后刷新设备项中依赖语言资源的展示属性。
        /// </summary>
        public void RefreshLocalizedText()
        {
            OnPropertyChanged(nameof(DeviceState));
            OnPropertyChanged(nameof(ConnectionType));
            OnPropertyChanged(nameof(ConnectionTagBackground));
            OnPropertyChanged(nameof(ConnectionTagForeground));
            OnPropertyChanged(nameof(TaskDetailDisplayMessage));
        }

        /// <summary>
        /// 获取当前语言下的设备状态文案。
        /// </summary>
        /// <param name="state">ADB 设备状态。</param>
        /// <returns>本地化后的设备状态文案。</returns>
        private static string GetDeviceStateText(JXAdbCore.Enums.DeviceState state)
        {
            switch (state)
            {
                case JXAdbCore.Enums.DeviceState.Online:
                    return GetLangText("DeviceState_System");
                case JXAdbCore.Enums.DeviceState.Recovery:
                    return GetLangText("DeviceState_Recovery");
                case JXAdbCore.Enums.DeviceState.BootLoader:
                    return "Download";
                case JXAdbCore.Enums.DeviceState.Sideload:
                    return GetLangText("DeviceState_Sideload");
                case JXAdbCore.Enums.DeviceState.Offline:
                    return GetLangText("DeviceState_Offline");
                case JXAdbCore.Enums.DeviceState.Unauthorized:
                    return GetLangText("DeviceState_Unauthorized");
                case JXAdbCore.Enums.DeviceState.NoPermissions:
                    return GetLangText("DeviceState_NoPermission");
                default:
                    return GetLangText("DeviceState_Unknown");
            }
        }

        /// <summary>
        /// 获取当前语言下的设备项文案。
        /// </summary>
        /// <param name="key">语言资源键。</param>
        /// <returns>当前语言对应文案。</returns>
        private static string GetLangText(string key)
        {
            return LocalizationService.Instance.GetString(string.Empty, key);
        }

        #region 调用任务的方法
        /// <summary>
        /// 启动常用 ADB 指令。
        /// </summary>
        public void StartAdbCommand(AdbCommandModel adbModel) => Service.StartAdbCommand(adbModel);

        /// <summary>
        /// 执行 TWRP 指令。
        /// </summary>
        public void ExecuteTWRPCommand(CommandModel commandModel) => Service.ExecuteTWRPCommand(commandModel);
        /// <summary>
        /// 刷入系统更新包。
        /// </summary>
        public void UpdateSystem(string filePath) => Service.UpdateSystem(filePath);

        /// <summary>
        /// 刷入系统更新包，并按需清除数据。
        /// </summary>
        /// <param name="filePath">更新包路径。</param>
        /// <param name="wipeData">是否清除数据。</param>
        public void UpdateSystem(string filePath, bool wipeData) => Service.UpdateSystem(filePath, wipeData);
        /// <summary>
        /// 删除设备中的更新文件。
        /// </summary>
        public void DeleteUpdateFile() => Service.DeleteUpdateFile();

        /// <summary>
        /// 恢复出厂设置。
        /// </summary>
        public void RestoreFactory() => Service.RestoreFactory();
        #endregion

        #region 样式
        private SolidColorBrush itemBackgroundColor;
        /// <summary>
        /// 旧列表项背景色，保留给现有其它页面兼容使用。
        /// </summary>
        public SolidColorBrush ItemBackgroundColor
        {
            get
            {

                return IsSelect ?
                    (SolidColorBrush)new BrushConverter().ConvertFrom("#2D77FC")
                  : new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
            }
        }

        private SolidColorBrush itemForegroundColor;
        /// <summary>
        /// 旧列表项前景色，保留给现有其它页面兼容使用。
        /// </summary>
        public SolidColorBrush ItemForegroundColor
        {
            get
            {
                return IsSelect ?
                    (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFF")
                  : (SolidColorBrush)new BrushConverter().ConvertFrom("#252525");
            }
        }

        /// <summary>
        /// 旧列表项边框厚度，保留给现有其它页面兼容使用。
        /// </summary>
        public int BorderThickness
        {
            get
            {
                return IsSelect ? 2 : 0;
            }
        }

        /// <summary>
        /// 刷新旧列表项背景色。
        /// </summary>
        public void RefreshItemBackgroundColor()
        {
            OnPropertyChanged(nameof(ItemBackgroundColor));
        }

        /// <summary>
        /// 刷新旧列表项前景色。
        /// </summary>
        public void RefreshItemForegroundColor()
        {
            OnPropertyChanged(nameof(ItemForegroundColor));
        }

        /// <summary>
        /// 刷新旧列表项边框厚度。
        /// </summary>
        public void RefreshItemBorderThickness()
        {
            OnPropertyChanged(nameof(BorderThickness));
        }

        private static SolidColorBrush CreateBrush(string colorHex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex);
            brush.Freeze();
            return brush;
        }
        #endregion
    }
}
