using CommunityToolkit.Mvvm.ComponentModel;
using JiXingFlashTool.Model;
using JiXingFlashTool.Models;
using JiXingFlashTool.Services;
using JiXingFlashTool.Utils;
using LanguageCore;
using System;
using TaskCore.Abstractions;
using TaskCore.Tasks;

namespace JiXingFlashTool.ItemViewModel
{
    /// <summary>
    /// 设备列表中的单个设备展示模型，负责承载主页面表格展示所需的状态与样式数据。
    /// </summary>
    public partial class DeviceItemViewModel : ObservableObject
    {
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

        /// <summary>
        /// 列表序号，供主页面表格展示。
        /// </summary>
        [ObservableProperty]
        private int displayIndex;

        /// <summary>
        /// 设备序列号，网络连接时仅展示主机地址部分。
        /// </summary>
        public string Serial => Device.Serial;

        /// <summary>
        /// 设备型号。
        /// </summary>
        public string Model => Device.Model;

        /// <summary>
        /// 设备展示名称。
        /// </summary>
        public string ModelName => Device.Name;

        /// <summary>
        /// 设备品牌，对应 ro.product.brand。
        /// </summary>
        public string Brand => Device.Brand;

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
        public string SystemVersion => Device.PolestarVersion;

        /// <summary>
        /// 设备名称。
        /// </summary>
        public string Name => Device.Name;

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
        public bool IsEthernetConnection => Device.Serial != null && Device.Serial.Contains(":");

        /// <summary>
        /// 当前设备状态，供界面根据状态切换展示样式。
        /// </summary>
        public JXAdbCore.Enums.DeviceState CurrentDeviceState => Device.State;

        /// <summary>
        /// 编译日期。
        /// </summary>
        public string BuildDate {
            get {
                return device.BuildDate;
            }
        }

        /// <summary>
        /// 当前设备是否被选中。
        /// </summary>
        [ObservableProperty]
        private bool isSelect;

        /// <summary>
        /// 断开连接时是否保留该设备项，不立即从列表移除。
        /// </summary>
        [ObservableProperty]
        private bool isKeepWhenDisconnected;

        /// <summary>
        /// 当前任务详情文案。
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TaskDetailDisplayMessage))]
        private string taskDetailMessage;

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
            OnPropertyChanged(nameof(CurrentDeviceState));
            OnPropertyChanged(nameof(IsEthernetConnection));
            OnPropertyChanged(nameof(TaskDetailDisplayMessage));
        }

        /// <summary>
        /// 创建当前设备行的任务观察者，使任务日志直接回写到当前行。
        /// </summary>
        /// <returns>当前设备行专用的任务观察者。</returns>
        public ITaskObserver CreateTaskObserver()
        {
            return new DeviceTaskObserver(this);
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
        /// <summary>
        /// 刷新设备模型更新后的展示属性。
        /// </summary>
        public void RefreshDeviceProperties() {
            CommonTool.RunOnUiThread(() => {
                OnPropertyChanged(nameof(AndroidVersion));
                OnPropertyChanged(nameof(SystemVersion));
                OnPropertyChanged(nameof(BuildDate));
                OnPropertyChanged(nameof(Brand));
                OnPropertyChanged(nameof(Model));
                OnPropertyChanged(nameof(ModelName));
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(DeviceState));
                OnPropertyChanged(nameof(CurrentDeviceState));
                OnPropertyChanged(nameof(ConnectionType));
                OnPropertyChanged(nameof(IsEthernetConnection));
            });
        }

        /// <summary>
        /// 接收当前设备任务的日志，并在 UI 线程更新任务消息。
        /// </summary>
        /// <param name="log">任务日志。</param>
        private void UpdateTaskDetailMessage(TaskLog log)
        {
            if (log == null)
            {
                return;
            }

            CommonTool.RunOnUiThread(() => TaskDetailMessage = TaskLogLocalizationService.NormalizeMessage(log.Message ?? string.Empty));
        }

        /// <summary>
        /// 当前设备行的任务观察者，只接收入队时绑定到本行的任务回调。
        /// </summary>
        private sealed class DeviceTaskObserver : ITaskObserver
        {
            private readonly DeviceItemViewModel _owner;

            /// <summary>
            /// 使用目标设备行初始化任务观察者。
            /// </summary>
            /// <param name="owner">目标设备行。</param>
            public DeviceTaskObserver(DeviceItemViewModel owner)
            {
                _owner = owner;
            }

            /// <summary>
            /// 接收任务开始通知。
            /// </summary>
            public void OnStarted(string deviceId, Guid taskId, string taskType)
            {
            }

            /// <summary>
            /// 接收任务进度通知。
            /// </summary>
            public void OnProgress(string deviceId, Guid taskId, TaskProgress progress)
            {
            }

            /// <summary>
            /// 接收任务日志通知。
            /// </summary>
            public void OnLog(string deviceId, Guid taskId, TaskLog log)
            {
                _owner.UpdateTaskDetailMessage(log);
            }

            /// <summary>
            /// 接收任务完成通知。
            /// </summary>
            public void OnCompleted(string deviceId, Guid taskId, string taskType)
            {
            }

            /// <summary>
            /// 接收任务取消通知。
            /// </summary>
            public void OnCanceled(string deviceId, Guid taskId, string taskType)
            {
            }

            /// <summary>
            /// 接收任务失败通知。
            /// </summary>
            public void OnFailed(string deviceId, Guid taskId, string taskType, Exception ex)
            {
            }
        }

    }
}
