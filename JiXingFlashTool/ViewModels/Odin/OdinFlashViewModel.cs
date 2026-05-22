using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.ItemViewModel.Odin;
using JiXingFlashTool.Views.Odin;
using JXHeimdall.Models;
using JXHeimdall.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace JiXingFlashTool.ViewModels.Odin
{
    /// <summary>
    /// Odin 刷机页面 ViewModel，负责 Download 设备列表、固件分配和 Heimdall 刷机流程编排。
    /// </summary>
    public sealed class OdinFlashViewModel : ObservableObject
    {
        private readonly HeimdallProcessService _processService = new HeimdallProcessService();
        private readonly HeimdallFirmwarePackageService _firmwarePackageService = new HeimdallFirmwarePackageService();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private string _searchKeyword = string.Empty;
        private bool _isLoading;
        private bool _isFlashing;
        private bool _isSelectAll;
        private string _summaryText = "共 0 台设备 · 等待 0 台 · 刷机中 0 台 · 完成 0 台";

        /// <summary>
        /// 初始化 Odin 刷机页面 ViewModel。
        /// </summary>
        public OdinFlashViewModel()
        {
            DeviceService = new HeimdallDeviceService(_processService);
            PitService = new HeimdallPitService(_processService);
            FlashService = new HeimdallFlashService(_processService, PitService, _firmwarePackageService);
            RefreshDevicesCommand = new AsyncRelayCommand(RefreshDevicesAsync);
            OpenFirmwareDialogCommand = new RelayCommand(OpenFirmwareDialog, HasSelectedDevices);
            StopSelectedDevicesCommand = new RelayCommand(StopSelectedDevices, HasSelectedDevices);
            StartFlashCommand = new AsyncRelayCommand(StartFlashAsync, CanStartFlash);
            FlashTwrpCommand = new AsyncRelayCommand(FlashTwrpAsync, CanStartTwrpFlash);
        }

        /// <summary>
        /// Download 模式设备集合。
        /// </summary>
        public ObservableCollection<OdinDeviceItemViewModel> Devices { get; } = new ObservableCollection<OdinDeviceItemViewModel>();

        /// <summary>
        /// Heimdall 设备服务。
        /// </summary>
        public HeimdallDeviceService DeviceService { get; }

        /// <summary>
        /// Heimdall PIT 服务。
        /// </summary>
        public HeimdallPitService PitService { get; }

        /// <summary>
        /// Heimdall 刷机服务。
        /// </summary>
        public HeimdallFlashService FlashService { get; }

        /// <summary>
        /// 搜索关键字。
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>
        /// 是否正在加载设备列表。
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 是否正在执行刷机流程。
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
        /// 是否显示选择固件按钮。
        /// </summary>
        public bool HasSelectedDevicesForFirmware => SelectedDeviceCount > 0;

        /// <summary>
        /// 选择固件按钮显示文本。
        /// </summary>
        public string SelectFirmwareButtonText => SelectedDeviceCount > 0 ? $"选择固件（{SelectedDeviceCount} 台）" : "选择固件";

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
        /// 打开固件选择弹窗命令。
        /// </summary>
        public RelayCommand OpenFirmwareDialogCommand { get; }

        /// <summary>
        /// 停止当前选中设备刷机流程命令。
        /// </summary>
        public RelayCommand StopSelectedDevicesCommand { get; }

        /// <summary>
        /// 开始普通 Odin 固件刷入命令。
        /// </summary>
        public AsyncRelayCommand StartFlashCommand { get; }

        /// <summary>
        /// 按 TWRP 流程刷入并进入 Recovery 命令。
        /// </summary>
        public AsyncRelayCommand FlashTwrpCommand { get; }

        /// <summary>
        /// 刷新 Download 模式设备列表。
        /// </summary>
        /// <returns>异步任务。</returns>
        public async Task RefreshDevicesAsync()
        {
            IsLoading = true;
            try
            {
                var devices = await DeviceService.GetDownloadModeDevicesAsync(_cancellationTokenSource.Token);
                Devices.Clear();
                foreach (var device in devices)
                {
                    var item = new OdinDeviceItemViewModel(device);
                    item.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(OdinDeviceItemViewModel.IsSelected))
                        {
                            RefreshSelectionState();
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
        /// 打开固件选择弹窗，并把选择结果分配给当前选中设备。
        /// </summary>
        private void OpenFirmwareDialog()
        {
            var selectedDevices = Devices.Where(item => item.IsSelected).ToList();
            if (selectedDevices.Count == 0)
            {
                Growl.Warning("请先选择需要分配固件的设备。");
                return;
            }

            var viewModel = new OdinFirmwareSelectionDialogViewModel(selectedDevices.Count, dialogViewModel =>
            {
                foreach (var device in selectedDevices)
                {
                    device.AssignFirmware(
                        dialogViewModel.BlFilePath,
                        dialogViewModel.ApFilePath,
                        dialogViewModel.CscFilePath,
                        dialogViewModel.UserdataFilePath);
                }

                RefreshCommandState();
                RefreshSummary();
            });
            var view = new OdinFirmwareSelectionDialogView
            {
                DataContext = viewModel
            };
            viewModel.Dialog = Dialog.Show(view);
        }

        /// <summary>
        /// 停止当前选中设备的刷机流程并更新列表状态。
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
                device.StatusText = "已停止";
            }

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
            return !IsFlashing && Devices.Any(item => item.IsSelected && !string.IsNullOrWhiteSpace(item.BlFilePath) && !string.IsNullOrWhiteSpace(item.ApFilePath));
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
        /// 启动普通 Odin 固件刷入流程。
        /// </summary>
        /// <returns>异步任务。</returns>
        private async Task StartFlashAsync()
        {
            await FlashSelectedDevicesAsync(false);
        }

        /// <summary>
        /// 启动 TWRP 刷入流程。
        /// </summary>
        /// <returns>异步任务。</returns>
        private async Task FlashTwrpAsync()
        {
            await FlashSelectedDevicesAsync(true);
        }

        /// <summary>
        /// 对当前选中设备执行刷机流程。
        /// </summary>
        /// <param name="twrpMode">是否按 TWRP 模式刷入。</param>
        /// <returns>异步任务。</returns>
        private async Task FlashSelectedDevicesAsync(bool twrpMode)
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
                    device.StatusText = "刷机中";
                    var request = BuildRequest(device, twrpMode);
                    var result = twrpMode
                        ? await FlashService.FlashTwrpAndBootRecoveryAsync(request, _cancellationTokenSource.Token)
                        : await FlashService.FlashFirmwareAsync(request, _cancellationTokenSource.Token);
                    device.StatusText = result.IsSuccess ? "完成" : "失败";
                }

                RefreshSummary();
            }
            catch (Exception ex)
            {
                Growl.Error("刷机流程失败：" + ex.Message);
            }
            finally
            {
                IsFlashing = false;
                RefreshCommandState();
            }
        }

        /// <summary>
        /// 根据设备固件分配构建 Heimdall 刷机请求。
        /// </summary>
        /// <param name="device">待刷入设备。</param>
        /// <param name="twrpMode">是否为 TWRP 刷入模式。</param>
        /// <returns>Heimdall 刷机请求。</returns>
        private HeimdallFlashRequest BuildRequest(OdinDeviceItemViewModel device, bool twrpMode)
        {
            var request = new HeimdallFlashRequest
            {
                Device = device.Device,
                RebootToRecoveryAfterFlash = twrpMode,
                Log = message => UpdateDeviceStatus(device, message)
            };
            if (!twrpMode && !string.IsNullOrWhiteSpace(device.BlFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.BL] = device.BlFilePath;
            }

            if (!string.IsNullOrWhiteSpace(device.ApFilePath))
            {
                request.FirmwareFiles[twrpMode ? HeimdallFirmwareSlot.TWRP : HeimdallFirmwareSlot.AP] = device.ApFilePath;
            }

            if (!twrpMode && !string.IsNullOrWhiteSpace(device.CscFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.CSC] = device.CscFilePath;
            }

            if (!twrpMode && !string.IsNullOrWhiteSpace(device.UserdataFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.USERDATA] = device.UserdataFilePath;
            }

            return request;
        }

        /// <summary>
        /// 在 UI 线程更新单台设备刷机状态，避免 Heimdall 后台输出回调跨线程触发绑定通知。
        /// </summary>
        /// <param name="device">设备项。</param>
        /// <param name="message">状态消息。</param>
        private static void UpdateDeviceStatus(OdinDeviceItemViewModel device, string message)
        {
            if (device == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                device.StatusText = message;
                return;
            }

            dispatcher.BeginInvoke(new Action(() => device.StatusText = message));
        }

        /// <summary>
        /// 刷新 Odin 页面选择状态相关属性。
        /// </summary>
        private void RefreshSelectionState()
        {
            OnPropertyChanged(nameof(SelectedDeviceCount));
            OnPropertyChanged(nameof(HasSelectedDevicesForFirmware));
            OnPropertyChanged(nameof(SelectFirmwareButtonText));
            OnPropertyChanged(nameof(StopButtonText));
            RefreshCommandState();
        }

        /// <summary>
        /// 刷新 Odin 页面相关命令可用状态。
        /// </summary>
        private void RefreshCommandState()
        {
            OpenFirmwareDialogCommand.NotifyCanExecuteChanged();
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
            var flashingCount = Devices.Count(item => item.StatusText == "刷机中");
            var finishedCount = Devices.Count(item => item.StatusText == "完成");
            var waitingCount = totalCount - flashingCount - finishedCount;
            SummaryText = $"共 {totalCount} 台设备 · 等待 {waitingCount} 台 · 刷机中 {flashingCount} 台 · 完成 {finishedCount} 台";
        }
    }
}
