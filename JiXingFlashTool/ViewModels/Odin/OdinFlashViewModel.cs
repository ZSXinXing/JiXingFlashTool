using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.Enums;
using JiXingFlashTool.ItemViewModel.Odin;
using JiXingFlashTool.Model.Payload;
using JiXingFlashTool.Services;
using JiXingFlashTool.TaskCoreBridge;
using JiXingFlashTool.Tasks;
using JXHeimdall.Models;
using JXHeimdall.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TaskCore.Scheduling;
using TaskCore.Sessions;
using TaskCore.Tasks;

namespace JiXingFlashTool.ViewModels.Odin
{
    /// <summary>
    /// Odin 刷机页面 ViewModel，负责 Download 设备列表、固件分配和 TaskCore 刷机任务入队。
    /// </summary>
    public sealed class OdinFlashViewModel : ObservableObject
    {
        private readonly HeimdallProcessService _processService = new HeimdallProcessService();
        private readonly IDeviceTaskScheduler _taskScheduler;
        private readonly Dictionary<string, HeimdallDeviceModel> _downloadDevicesByTaskId = new Dictionary<string, HeimdallDeviceModel>(StringComparer.OrdinalIgnoreCase);
        private ManagementEventWatcher _downloadDeviceChangeWatcher;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private string _searchKeyword = string.Empty;
        private bool _isLoading;
        private bool _isFlashing;
        private bool _isSelectAll;
        private int _downloadDeviceRefreshVersion;
        private string _summaryText = "共 0 台设备 · 等待 0 台 · 刷机中 0 台 · 完成 0 台";

        /// <summary>
        /// 初始化 Odin 刷机页面 ViewModel。
        /// </summary>
        public OdinFlashViewModel()
        {
            DeviceService = new HeimdallDeviceService(_processService);
            var sessionFactory = new HeimdallDeviceSessionFactory(ResolveDeviceByTaskId);
            _taskScheduler = new DeviceTaskScheduler(new EphemeralDeviceSessionProvider(sessionFactory));
            _taskScheduler.Log += OnTaskSchedulerLog;
            _taskScheduler.TaskStateChanged += OnTaskStateChanged;
            FirmwareSelection = new OdinFirmwareSelectionDialogViewModel(0, null);
            FirmwareSelection.PropertyChanged += (_, __) => RefreshCommandState();
            RefreshDevicesCommand = new AsyncRelayCommand(RefreshDevicesAsync);
            StopSelectedDevicesCommand = new RelayCommand(StopSelectedDevices, HasSelectedDevices);
            StartFlashCommand = new AsyncRelayCommand(StartFlashAsync, CanStartFlash);
            FlashTwrpCommand = new AsyncRelayCommand(FlashTwrpAsync, CanStartTwrpFlash);
            StartDownloadDeviceChangeWatcher();
        }

        /// <summary>
        /// Download 模式设备集合。
        /// </summary>
        public ObservableCollection<OdinDeviceItemViewModel> Devices { get; } = new ObservableCollection<OdinDeviceItemViewModel>();

        /// <summary>
        /// Heimdall 设备扫描服务。
        /// </summary>
        public HeimdallDeviceService DeviceService { get; }

        /// <summary>
        /// Odin 页面中的固件选择区域。
        /// </summary>
        public OdinFirmwareSelectionDialogViewModel FirmwareSelection { get; }

        /// <summary>
        /// 搜索关键字，保留属性以兼容页面旧绑定。
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>
        /// 是否正在加载 Download 设备列表。
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 是否存在正在执行或排队的 Odin 刷机任务。
        /// </summary>
        public bool IsFlashing
        {
            get => _isFlashing;
            set
            {
                if (SetProperty(ref _isFlashing, value))
                {
                    StartFlashCommand.NotifyCanExecuteChanged();
                    FlashTwrpCommand.NotifyCanExecuteChanged();
                    StopSelectedDevicesCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 是否全选当前设备列表。
        /// </summary>
        public bool IsSelectAll
        {
            get => _isSelectAll;
            set
            {
                if (SetProperty(ref _isSelectAll, value))
                {
                    foreach (var device in Devices)
                    {
                        device.IsSelected = value;
                    }

                    RefreshSelectionState();
                }
            }
        }

        /// <summary>
        /// 当前选中的设备数量。
        /// </summary>
        public int SelectedDeviceCount => Devices.Count(item => item.IsSelected);

        /// <summary>
        /// 是否存在已选中设备。
        /// </summary>
        public bool HasSelectedDevicesForFirmware => SelectedDeviceCount > 0;

        /// <summary>
        /// 刷入按钮显示文本。
        /// </summary>
        public string FlashButtonText => SelectedDeviceCount > 0 ? $"刷入（{SelectedDeviceCount} 台）" : "刷入";

        /// <summary>
        /// 停止按钮显示文本。
        /// </summary>
        public string StopButtonText => SelectedDeviceCount > 0 ? $"停止（{SelectedDeviceCount} 台）" : "停止";

        /// <summary>
        /// 底部设备统计文本。
        /// </summary>
        public string SummaryText
        {
            get => _summaryText;
            set => SetProperty(ref _summaryText, value);
        }

        /// <summary>
        /// 刷新 Download 设备命令。
        /// </summary>
        public AsyncRelayCommand RefreshDevicesCommand { get; }

        /// <summary>
        /// 停止当前选中设备刷机任务命令。
        /// </summary>
        public RelayCommand StopSelectedDevicesCommand { get; }

        /// <summary>
        /// 开始普通 Odin 固件刷入命令，完成后由 Heimdall 默认流程重启。
        /// </summary>
        public AsyncRelayCommand StartFlashCommand { get; }

        /// <summary>
        /// 刷入 TWRP 并进入 Recovery 命令。
        /// </summary>
        public AsyncRelayCommand FlashTwrpCommand { get; }

        /// <summary>
        /// 刷新 Download 模式设备列表。
        /// </summary>
        /// <returns>异步刷新任务。</returns>
        public async Task RefreshDevicesAsync()
        {
            IsLoading = true;
            try
            {
                var devices = await DeviceService.GetDownloadModeDevicesAsync(_cancellationTokenSource.Token);
                var existingDevicesByTaskId = Devices
                    .GroupBy(GetTaskDeviceId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
                Devices.Clear();
                _downloadDevicesByTaskId.Clear();
                foreach (var device in devices)
                {
                    var item = new OdinDeviceItemViewModel(device);
                    var taskDeviceId = GetTaskDeviceId(item);
                    if (existingDevicesByTaskId.TryGetValue(taskDeviceId, out var existingItem))
                    {
                        ApplyExistingDeviceState(item, existingItem);
                    }

                    _downloadDevicesByTaskId[taskDeviceId] = device;
                    item.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(OdinDeviceItemViewModel.IsSelected))
                        {
                            RefreshSelectionState();
                            return;
                        }

                        if (args.PropertyName == nameof(OdinDeviceItemViewModel.ApFilePath) ||
                            args.PropertyName == nameof(OdinDeviceItemViewModel.BlFilePath) ||
                            args.PropertyName == nameof(OdinDeviceItemViewModel.TwrpFilePath) ||
                            args.PropertyName == nameof(OdinDeviceItemViewModel.CpFilePath) ||
                            args.PropertyName == nameof(OdinDeviceItemViewModel.CscFilePath) ||
                            args.PropertyName == nameof(OdinDeviceItemViewModel.UserdataFilePath))
                        {
                            RefreshCommandState();
                        }
                    };
                    Devices.Add(item);
                }

                RefreshSelectionState();
                RefreshSummary();
            }
            catch (Exception ex)
            {
                Growl.Error("获取 Download 模式设备失败：" + ex.Message);
            }
            finally
            {
                IsLoading = false;
                RefreshCommandState();
            }
        }

        /// <summary>
        /// 设备刷新重建列表行时保留原有刷机状态和固件分配，避免失败信息被自动刷新清空。
        /// </summary>
        /// <param name="targetItem">刷新后新创建的设备行。</param>
        /// <param name="sourceItem">刷新前已有的设备行。</param>
        private static void ApplyExistingDeviceState(OdinDeviceItemViewModel targetItem, OdinDeviceItemViewModel sourceItem)
        {
            if (targetItem == null || sourceItem == null)
            {
                return;
            }

            targetItem.AssignFirmware(
                sourceItem.BlFilePath,
                sourceItem.ApFilePath,
                sourceItem.TwrpFilePath,
                sourceItem.SystemPackageFilePath,
                sourceItem.WipeDataBeforeSystemFlash,
                sourceItem.WipeSystemBeforeSystemFlash,
                sourceItem.FormatDataBeforeSystemFlash,
                sourceItem.CpFilePath,
                sourceItem.CscFilePath,
                sourceItem.UserdataFilePath);
            targetItem.IsSelected = sourceItem.IsSelected;
            targetItem.StatusText = sourceItem.StatusText;
        }

        /// <summary>
        /// 释放 Odin 页面设备监听资源，避免页面卸载后继续持有 WMI 事件订阅。
        /// </summary>
        public void Dispose()
        {
            try
            {
                _downloadDeviceChangeWatcher?.Stop();
                _downloadDeviceChangeWatcher?.Dispose();
                _downloadDeviceChangeWatcher = null;
            }
            catch (ManagementException)
            {
            }
        }

        /// <summary>
        /// 停止当前选中设备的 TaskCore Odin 刷机任务。
        /// </summary>
        private void StopSelectedDevices()
        {
            var selectedDevices = Devices.Where(item => item.IsSelected).ToList();
            if (selectedDevices.Count == 0)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            foreach (var device in selectedDevices)
            {
                var taskDeviceId = GetTaskDeviceId(device);
                _ = _taskScheduler.CancelCurrentAsync(taskDeviceId);
                CancelQueuedTasks(taskDeviceId);
                device.StatusText = "已停止";
            }

            IsFlashing = Devices.Any(IsDeviceTaskActive);
            RefreshCommandState();
            RefreshSummary();
        }

        /// <summary>
        /// 判断是否已经选中设备。
        /// </summary>
        /// <returns>存在选中设备时返回 true。</returns>
        private bool HasSelectedDevices()
        {
            return SelectedDeviceCount > 0;
        }

        /// <summary>
        /// 判断是否允许启动普通 Odin 固件刷入。
        /// </summary>
        /// <returns>可刷入时返回 true。</returns>
        private bool CanStartFlash()
        {
            return !IsFlashing && HasSelectedDevices() && FirmwareSelection.HasApFile;
        }

        /// <summary>
        /// 判断是否允许启动 TWRP 刷入。
        /// </summary>
        /// <returns>可刷入 TWRP 时返回 true。</returns>
        private bool CanStartTwrpFlash()
        {
            return !IsFlashing && Devices.Any(item => item.IsSelected && !string.IsNullOrWhiteSpace(item.ApFilePath));
        }

        /// <summary>
        /// 启动普通 Odin 固件刷入任务。
        /// </summary>
        /// <returns>异步入队任务。</returns>
        private async Task StartFlashAsync()
        {
            AssignSelectedFirmware();
            await EnqueueSelectedDevicesAsync(ResolveFlashMode(FirmwareSelection));
        }

        /// <summary>
        /// 将页面当前选择的固件分配给所有勾选设备。
        /// </summary>
        private void AssignSelectedFirmware()
        {
            foreach (var device in Devices.Where(item => item.IsSelected))
            {
                device.AssignFirmware(
                    FirmwareSelection.BlFilePath,
                    FirmwareSelection.ApFilePath,
                    FirmwareSelection.TwrpFilePath,
                    FirmwareSelection.SystemPackageFilePath,
                    FirmwareSelection.WipeDataBeforeSystemFlash,
                    FirmwareSelection.WipeSystemBeforeSystemFlash,
                    FirmwareSelection.FormatDataBeforeSystemFlash,
                    FirmwareSelection.CpFilePath,
                    FirmwareSelection.CscFilePath,
                    FirmwareSelection.UserdataFilePath);
            }

            RefreshCommandState();
            RefreshSummary();
        }

        /// <summary>
        /// 启动 TWRP 刷入并进入 Recovery 任务。
        /// </summary>
        /// <returns>异步入队任务。</returns>
        private async Task FlashTwrpAsync()
        {
            await EnqueueSelectedDevicesAsync(OdinFlashMode.TwrpRebootRecovery);
        }

        /// <summary>
        /// 将当前选中设备的 Odin 刷机任务加入 TaskCore 队列。
        /// </summary>
        /// <param name="flashMode">刷机模式。</param>
        /// <returns>异步入队任务。</returns>
        private async Task EnqueueSelectedDevicesAsync(OdinFlashMode? forcedFlashMode = null)
        {
            var selectedDevices = Devices.Where(item => item.IsSelected).ToList();
            if (selectedDevices.Count == 0)
            {
                Growl.Warning("请先选择需要刷入的设备。");
                return;
            }

            IsFlashing = true;
            try
            {
                foreach (var device in selectedDevices)
                {
                    device.StatusText = "等待刷机";
                    var flashMode = forcedFlashMode ?? ResolveFlashMode(device);
                    await _taskScheduler.EnqueueAsync(
                        GetTaskDeviceId(device),
                        new OdinFlashTask(),
                        BuildPayload(device, flashMode),
                        detail: OdinFlashModeResolver.GetTaskDetailText(flashMode));
                }

                RefreshSummary();
            }
            catch (Exception ex)
            {
                Growl.Error("刷机任务入队失败：" + ex.Message);
                IsFlashing = Devices.Any(IsDeviceTaskActive);
            }
            finally
            {
                RefreshCommandState();
            }
        }

        /// <summary>
        /// 根据页面选择内容判断本次刷入模式，单独选择 TWRP AP 包时进入 TWRP Recovery 流程。
        /// </summary>
        /// <param name="selectionViewModel">固件选择区域 ViewModel。</param>
        /// <returns>本次 Odin 刷机模式。</returns>
        private static OdinFlashMode ResolveFlashMode(OdinFirmwareSelectionDialogViewModel selectionViewModel)
        {
            return OdinFlashModeResolver.Resolve(
                selectionViewModel.HasBlFile,
                selectionViewModel.HasApFile,
                selectionViewModel.ApFileName,
                selectionViewModel.HasTwrpFile,
                selectionViewModel.HasSystemPackageFile,
                selectionViewModel.HasCpFile,
                selectionViewModel.HasCscFile,
                selectionViewModel.HasUserdataFile);
        }

        /// <summary>
        /// 根据设备已经分配的固件内容判断刷入模式，确保开始刷入按钮不会丢失 TWRP 安装系统流程。
        /// </summary>
        /// <param name="device">已分配固件的 Odin 设备项。</param>
        /// <returns>当前设备应执行的 Odin 刷机模式。</returns>
        private static OdinFlashMode ResolveFlashMode(OdinDeviceItemViewModel device)
        {
            return OdinFlashModeResolver.Resolve(
                !string.IsNullOrWhiteSpace(device.BlFilePath),
                !string.IsNullOrWhiteSpace(device.ApFilePath),
                device.ApFileName,
                !string.IsNullOrWhiteSpace(device.TwrpFilePath),
                !string.IsNullOrWhiteSpace(device.SystemPackageFilePath),
                !string.IsNullOrWhiteSpace(device.CpFilePath),
                !string.IsNullOrWhiteSpace(device.CscFilePath),
                !string.IsNullOrWhiteSpace(device.UserdataFilePath));
        }

        /// <summary>
        /// 根据设备固件分配构建 Odin 刷机任务参数。
        /// </summary>
        /// <param name="device">待刷入设备。</param>
        /// <param name="flashMode">刷机模式。</param>
        /// <returns>Odin 刷机任务参数。</returns>
        private static OdinFlashPayload BuildPayload(OdinDeviceItemViewModel device, OdinFlashMode flashMode)
        {
            return new OdinFlashPayload(
                device.Device,
                device.BlFilePath,
                device.ApFilePath,
                device.TwrpFilePath,
                device.SystemPackageFilePath,
                device.WipeDataBeforeSystemFlash,
                device.WipeSystemBeforeSystemFlash,
                device.FormatDataBeforeSystemFlash,
                device.CpFilePath,
                device.CscFilePath,
                device.UserdataFilePath,
                flashMode);
        }

        /// <summary>
        /// 接收 TaskCore 日志并同步到 Odin 设备行。
        /// </summary>
        /// <param name="deviceId">TaskCore 设备标识。</param>
        /// <param name="log">任务日志。</param>
        private void OnTaskSchedulerLog(string deviceId, TaskLog log)
        {
            RunOnUiThread(() => UpdateDeviceStatus(FindDeviceByTaskId(deviceId), GetTaskLogDisplayMessage(log)));
        }

        /// <summary>
        /// 获取 TaskCore 日志的列表展示文本，失败日志优先显示异常详情而不是通用 Error。
        /// </summary>
        /// <param name="log">TaskCore 任务日志。</param>
        /// <returns>适合任务详情列显示的文本。</returns>
        private static string GetTaskLogDisplayMessage(TaskLog log)
        {
            if (log == null)
            {
                return string.Empty;
            }

            if (string.Equals(log.Message, "Error", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(log.Level))
            {
                var firstLine = log.Level
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .FirstOrDefault();
                return NormalizeTaskDetailMessage(string.IsNullOrWhiteSpace(firstLine) ? log.Message : firstLine);
            }

            return NormalizeTaskDetailMessage(log.Message ?? string.Empty);
        }

        /// <summary>
        /// 清理 TaskCore 异常日志中的类型前缀，只保留 Odin 页面需要展示的任务详情。
        /// </summary>
        /// <param name="message">原始任务日志。</param>
        /// <returns>可直接展示给用户的任务详情。</returns>
        private static string NormalizeTaskDetailMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            const string invalidOperationPrefix = "System.InvalidOperationException: ";
            return message.StartsWith(invalidOperationPrefix, StringComparison.Ordinal)
                ? message.Substring(invalidOperationPrefix.Length)
                : message;
        }

        /// <summary>
        /// 接收 TaskCore 状态变更并刷新 Odin 设备行状态。
        /// </summary>
        /// <param name="args">任务状态变更参数。</param>
        private void OnTaskStateChanged(TaskStateChangedEvent args)
        {
            RunOnUiThread(() =>
            {
                var device = FindDeviceByTaskId(args.DeviceId);
                if (device == null)
                {
                    return;
                }

                device.StatusText = ConvertTaskStateToStatusText(args.State, args.Message);
                if (args.State == DeviceTaskState.Completed || args.State == DeviceTaskState.Failed || args.State == DeviceTaskState.Canceled)
                {
                    IsFlashing = Devices.Any(IsDeviceTaskActive);
                    RefreshSummary();
                    RefreshCommandState();
                    ScheduleDownloadDeviceRefresh();
                }
            });
        }

        /// <summary>
        /// 启动 Windows 设备变更监听，用于在 USB 插拔或手机离开 Download 模式后刷新 Odin 设备列表。
        /// </summary>
        private void StartDownloadDeviceChangeWatcher()
        {
            try
            {
                _downloadDeviceChangeWatcher = new ManagementEventWatcher(
                    new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3"));
                _downloadDeviceChangeWatcher.EventArrived += (_, __) => ScheduleDownloadDeviceRefresh();
                _downloadDeviceChangeWatcher.Start();
            }
            catch (ManagementException)
            {
            }
        }

        /// <summary>
        /// 延迟刷新 Download 设备列表，合并短时间内连续的 USB 设备变更事件。
        /// </summary>
        private void ScheduleDownloadDeviceRefresh()
        {
            var refreshVersion = Interlocked.Increment(ref _downloadDeviceRefreshVersion);
            _ = Task.Run(async () =>
            {
                await Task.Delay(1200).ConfigureAwait(false);
                if (refreshVersion != _downloadDeviceRefreshVersion)
                {
                    return;
                }

                RunOnUiThread(() =>
                {
                    if (!IsFlashing)
                    {
                        _ = RefreshDevicesAsync();
                    }
                });
            });
        }

        /// <summary>
        /// 将 TaskCore 状态转换为 Odin 列表状态文本。
        /// </summary>
        /// <param name="state">任务状态。</param>
        /// <param name="message">状态消息。</param>
        /// <returns>列表状态文本。</returns>
        private static string ConvertTaskStateToStatusText(DeviceTaskState state, string message)
        {
            switch (state)
            {
                case DeviceTaskState.Waiting:
                    return "等待刷机";
                case DeviceTaskState.Running:
                    return "刷机中";
                case DeviceTaskState.Completed:
                    return "完成";
                case DeviceTaskState.Canceled:
                    return "已停止";
                case DeviceTaskState.Failed:
                    return string.IsNullOrWhiteSpace(message) ? "失败" : message;
                default:
                    return message ?? string.Empty;
            }
        }

        /// <summary>
        /// 在 UI 线程更新单台设备刷机状态。
        /// </summary>
        /// <param name="device">设备项。</param>
        /// <param name="message">状态消息。</param>
        private static void UpdateDeviceStatus(OdinDeviceItemViewModel device, string message)
        {
            if (device == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            RunOnUiThread(() => device.StatusText = message);
        }

        /// <summary>
        /// 取消指定设备队列中的未开始任务。
        /// </summary>
        /// <param name="taskDeviceId">TaskCore 设备标识。</param>
        private void CancelQueuedTasks(string taskDeviceId)
        {
            var snapshot = _taskScheduler.GetSnapshot(taskDeviceId);
            foreach (var queuedItem in snapshot.Queued)
            {
                _ = _taskScheduler.CancelQueuedAsync(taskDeviceId, queuedItem.Id);
            }
        }

        /// <summary>
        /// 判断指定设备是否仍有正在执行或排队的任务。
        /// </summary>
        /// <param name="device">设备项。</param>
        /// <returns>存在活跃任务时返回 true。</returns>
        private bool IsDeviceTaskActive(OdinDeviceItemViewModel device)
        {
            var snapshot = _taskScheduler.GetSnapshot(GetTaskDeviceId(device));
            return snapshot.RunningId.HasValue || snapshot.Queued.Count > 0;
        }

        /// <summary>
        /// 根据 TaskCore 设备标识查找 Odin 设备行。
        /// </summary>
        /// <param name="taskDeviceId">TaskCore 设备标识。</param>
        /// <returns>匹配的 Odin 设备行。</returns>
        private OdinDeviceItemViewModel FindDeviceByTaskId(string taskDeviceId)
        {
            return Devices.FirstOrDefault(item => string.Equals(GetTaskDeviceId(item), taskDeviceId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 根据 TaskCore 设备标识解析 Download 模式设备模型。
        /// </summary>
        /// <param name="taskDeviceId">TaskCore 设备标识。</param>
        /// <returns>匹配的 Download 设备模型。</returns>
        private HeimdallDeviceModel ResolveDeviceByTaskId(string taskDeviceId)
        {
            return !string.IsNullOrWhiteSpace(taskDeviceId) && _downloadDevicesByTaskId.TryGetValue(taskDeviceId, out var device)
                ? device
                : null;
        }

        /// <summary>
        /// 获取 Download 设备在 TaskCore 中使用的稳定标识。
        /// </summary>
        /// <param name="device">Odin 设备行。</param>
        /// <returns>TaskCore 设备标识。</returns>
        private static string GetTaskDeviceId(OdinDeviceItemViewModel device)
        {
            var downloadDevice = device?.Device;
            if (!string.IsNullOrWhiteSpace(downloadDevice?.InstanceId))
            {
                return downloadDevice.InstanceId;
            }

            if (!string.IsNullOrWhiteSpace(downloadDevice?.ContainerId))
            {
                return downloadDevice.ContainerId;
            }

            return downloadDevice?.DisplayName ?? string.Empty;
        }

        /// <summary>
        /// 刷新 Odin 页面选择状态相关属性。
        /// </summary>
        private void RefreshSelectionState()
        {
            OnPropertyChanged(nameof(SelectedDeviceCount));
            OnPropertyChanged(nameof(HasSelectedDevicesForFirmware));
            OnPropertyChanged(nameof(FlashButtonText));
            OnPropertyChanged(nameof(StopButtonText));
            RefreshCommandState();
        }

        /// <summary>
        /// 刷新 Odin 页面相关命令可用状态。
        /// </summary>
        private void RefreshCommandState()
        {
            StopSelectedDevicesCommand.NotifyCanExecuteChanged();
            StartFlashCommand.NotifyCanExecuteChanged();
            FlashTwrpCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 刷新底部设备刷机统计文本。
        /// </summary>
        private void RefreshSummary()
        {
            var totalCount = Devices.Count;
            var flashingCount = Devices.Count(IsDeviceTaskActive);
            var finishedCount = Devices.Count(item => item.StatusText == "完成");
            var waitingCount = totalCount - flashingCount - finishedCount;
            SummaryText = $"共 {totalCount} 台设备 · 等待 {waitingCount} 台 · 刷机中 {flashingCount} 台 · 完成 {finishedCount} 台";
        }

        /// <summary>
        /// 在 UI 线程执行界面更新。
        /// </summary>
        /// <param name="action">需要执行的界面更新。</param>
        private static void RunOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action);
        }
    }
}
