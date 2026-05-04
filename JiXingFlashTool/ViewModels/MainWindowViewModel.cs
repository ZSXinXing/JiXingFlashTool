using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.EventArg;
using JiXingFlashTool.Model;
using JiXingFlashTool.Models;
using JiXingFlashTool.ObservableModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.ViewModels.AllScreen;
using JiXingFlashTool.Views;
using JXAdbCore.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;

namespace JiXingFlashTool.ViewModels
{
    /// <summary>
    /// 主页面视图模型，负责设备列表状态、筛选条件和主窗口入口命令的统一编排。
    /// </summary>
    public class MainWindowViewModel : ObservableObject
    {
        private string _title = string.Empty;
        private bool _isLoading;
        private bool _hasLoadError;
        private string _loadErrorMessage = "设备列表初始化失败，请检查设备连接状态。";
        private string _searchKeyword = string.Empty;
        private string _modelFilter = "全部型号";
        private string _connectionTypeFilter = "全部连接";
        private string _deviceStateFilter = "全部状态";
        private string _deviceSummaryText = "共 0 台设备";
        private string _appVersionText = "版本号：v1.0.0";
        private bool _isSelectAll;
        private bool _isSidebarExpanded = true;
        private int _listCount;

        /// <summary>
        /// 设备列表源集合。
        /// </summary>
        public ObservableCollection<DeviceObservableModel> DeviceCollection { get; } = new ObservableCollection<DeviceObservableModel>();

        /// <summary>
        /// 设备列表视图，用于承载筛选逻辑。
        /// </summary>
        public ICollectionView DeviceView { get; }

        /// <summary>
        /// 当前已连接设备名称筛选集合，保留现有结构兼容。
        /// </summary>
        public ObservableCollection<string> ConnectDeviceNameList { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 型号筛选集合。
        /// </summary>
        public ObservableCollection<string> ModelFilterList { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 连接方式筛选集合。
        /// </summary>
        public ObservableCollection<string> ConnectionTypeList { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 设备状态筛选集合。
        /// </summary>
        public ObservableCollection<string> DeviceStateList { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 当前窗口标题。
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 当前页面是否处于初始化中。
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    RaisePageStateChanged();
                }
            }
        }

        /// <summary>
        /// 当前页面是否存在加载失败状态。
        /// </summary>
        public bool HasLoadError
        {
            get => _hasLoadError;
            set
            {
                if (SetProperty(ref _hasLoadError, value))
                {
                    RaisePageStateChanged();
                }
            }
        }

        /// <summary>
        /// 加载失败时展示的错误文案。
        /// </summary>
        public string LoadErrorMessage
        {
            get => _loadErrorMessage;
            set => SetProperty(ref _loadErrorMessage, value);
        }

        /// <summary>
        /// 顶部和列表区共用的搜索关键字。
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    DeviceView.Refresh();
                }
            }
        }

        /// <summary>
        /// 当前型号筛选项。
        /// </summary>
        public string ModelFilter
        {
            get => _modelFilter;
            set
            {
                if (SetProperty(ref _modelFilter, value))
                {
                    DeviceView.Refresh();
                }
            }
        }

        /// <summary>
        /// 当前连接方式筛选项。
        /// </summary>
        public string ConnectionTypeFilter
        {
            get => _connectionTypeFilter;
            set
            {
                if (SetProperty(ref _connectionTypeFilter, value))
                {
                    DeviceView.Refresh();
                }
            }
        }

        /// <summary>
        /// 当前设备状态筛选项。
        /// </summary>
        public string DeviceStateFilter
        {
            get => _deviceStateFilter;
            set
            {
                if (SetProperty(ref _deviceStateFilter, value))
                {
                    DeviceView.Refresh();
                }
            }
        }

        /// <summary>
        /// 页面底部汇总文案。
        /// </summary>
        public string DeviceSummaryText
        {
            get => _deviceSummaryText;
            set => SetProperty(ref _deviceSummaryText, value);
        }

        /// <summary>
        /// 侧边栏版本文案。
        /// </summary>
        public string AppVersionText
        {
            get => _appVersionText;
            set => SetProperty(ref _appVersionText, value);
        }

        /// <summary>
        /// 侧边栏是否展开。
        /// </summary>
        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set
            {
                if (SetProperty(ref _isSidebarExpanded, value))
                {
                    OnPropertyChanged(nameof(SidebarPanelWidth));
                    OnPropertyChanged(nameof(SidebarHeaderHeight));
                    OnPropertyChanged(nameof(SidebarFooterHeight));
                    OnPropertyChanged(nameof(SidebarToggleIconGlyph));
                }
            }
        }

        /// <summary>
        /// 侧边栏当前宽度。
        /// </summary>
        public double SidebarPanelWidth => IsSidebarExpanded ? 223D : 55D;

        /// <summary>
        /// 侧边栏顶部容器当前高度。
        /// </summary>
        public double SidebarHeaderHeight => IsSidebarExpanded ? 63.563D : 57D;

        /// <summary>
        /// 侧边栏底部容器当前高度。
        /// </summary>
        public double SidebarFooterHeight => IsSidebarExpanded ? 173.5D : 198D;

        /// <summary>
        /// 侧边栏折叠按钮图标。
        /// </summary>
        public string SidebarToggleIconGlyph => IsSidebarExpanded ? "\uE76B" : "\uE76C";

        /// <summary>
        /// 当前设备总数。
        /// </summary>
        public int ListCount
        {
            get => _listCount;
            set
            {
                if (SetProperty(ref _listCount, value))
                {
                    OnPropertyChanged(nameof(SidebarDeviceCountText));
                }
            }
        }

        /// <summary>
        /// 当前是否为全选状态。
        /// </summary>
        public bool IsSelectAll
        {
            get => _isSelectAll;
            set
            {
                if (SetProperty(ref _isSelectAll, value))
                {
                    foreach (var deviceObservableModel in DeviceCollection)
                    {
                        deviceObservableModel.IsSelect = _isSelectAll;
                    }
                }
            }
        }

        /// <summary>
        /// 侧边栏设备统计文案。
        /// </summary>
        public string SidebarDeviceCountText => $"已连接设备：{ListCount} 台";

        /// <summary>
        /// 当前是否存在设备数据。
        /// </summary>
        public bool HasDevices => DeviceCollection.Count > 0;

        /// <summary>
        /// 是否显示空状态。
        /// </summary>
        public bool ShowEmptyState => !IsLoading && !HasLoadError && !HasDevices;

        /// <summary>
        /// 是否显示设备表格。
        /// </summary>
        public bool ShowDeviceTable => !IsLoading && !HasLoadError && HasDevices;

        private readonly List<DeviceObservableModel> _deviceList = new List<DeviceObservableModel>();

        /// <summary>
        /// 全选命令。
        /// </summary>
        public RelayCommand SelectAllCommand => new Lazy<RelayCommand>(() => new RelayCommand(SelectAll)).Value;

        /// <summary>
        /// 打开常用 ADB 命令窗口。
        /// </summary>
        public RelayCommand SystemShowViewCommand => new Lazy<RelayCommand>(() => new RelayCommand(ShowAdbCommandView)).Value;

        /// <summary>
        /// 打开 TWRP 命令窗口。
        /// </summary>
        public RelayCommand TWRPShowViewCommand => new Lazy<RelayCommand>(() => new RelayCommand(TWRPCommand)).Value;

        /// <summary>
        /// 打开系统更新窗口。
        /// </summary>
        public RelayCommand UpdateSystemCommand => new Lazy<RelayCommand>(() => new RelayCommand(UpdateSystem)).Value;

        /// <summary>
        /// 单控投屏命令。
        /// </summary>
        public RelayCommand SingleScreenCommand => new Lazy<RelayCommand>(() => new RelayCommand(SingleScreen)).Value;

        /// <summary>
        /// 群控投屏命令。
        /// </summary>
        public RelayCommand AllScreenCommand => new Lazy<RelayCommand>(() => new RelayCommand(AllScreen)).Value;

        /// <summary>
        /// 文件同步命令。
        /// </summary>
        public RelayCommand SyncFileCommand => new Lazy<RelayCommand>(() => new RelayCommand(SyncFile)).Value;

        /// <summary>
        /// 恢复出厂设置命令。
        /// </summary>
        public RelayCommand RestoreFactoryCommand => new Lazy<RelayCommand>(() => new RelayCommand(RestoreFactory)).Value;

        /// <summary>
        /// 刷新设备视图命令。
        /// </summary>
        public RelayCommand RefreshDeviceCommand => new Lazy<RelayCommand>(() => new RelayCommand(RefreshDeviceView)).Value;

        /// <summary>
        /// 切换侧边栏折叠状态命令。
        /// </summary>
        public RelayCommand ToggleSidebarCommand => new Lazy<RelayCommand>(() => new RelayCommand(ToggleSidebar)).Value;

        /// <summary>
        /// 初始化主页面视图模型。
        /// </summary>
        public MainWindowViewModel()
        {
            InitializeStaticFilters();
            InitializeTitle();

            DeviceView = CollectionViewSource.GetDefaultView(DeviceCollection);
            DeviceView.Filter = FilterDevice;
            DeviceCollection.CollectionChanged += OnDeviceCollectionChanged;

            DeviceService.Instance.DeviceDisconnected += PDeviceDisconnected;
            DeviceService.Instance.DeviceConnected += PDeviceConnected;
            DeviceService.Instance.DeviceChange += PDeviceChanage;

            RefreshDeviceSummary();
            RefreshConnectDeviceNameList();
        }

        /// <summary>
        /// 主窗口加载时执行初始化流程。
        /// </summary>
        public void ViewLoad()
        {
            IsLoading = true;
            HasLoadError = false;
            LoadErrorMessage = "设备列表初始化失败，请检查设备连接状态。";

            _ = Task.Run(() =>
            {
                try
                {
                    AppService.Instance.Read();
                    DeviceService.Instance.Init();
                    AdbService.Instance.StartServiceAsync();
                    CastScreenManageService.Instance.StartMonitor();

                    RunOnUiThread(() =>
                    {
                        IsLoading = false;
                        RefreshDeviceSummary();
                    });
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        IsLoading = false;
                        HasLoadError = true;
                        LoadErrorMessage = "设备服务初始化失败，请检查 ADB 服务或设备连接。";
                        Growl.Error(ex.Message);
                    });
                }
            });
        }

        /// <summary>
        /// 打开常用 ADB 命令窗口。
        /// </summary>
        private void ShowAdbCommandView()
        {
            var selectList = GetSelectedDevices();
            if (selectList.Count == 0)
            {
                Growl.Warning("请先选择手机");
                return;
            }

            var viewModel = new AdbListViewModel
            {
                SelectDeviceList = selectList
            };
            var dialog = new AdbListView
            {
                DataContext = viewModel
            };
            viewModel.dialog = Dialog.Show(dialog);
            viewModel.FinishDelegate = value =>
            {
                if (value == null)
                {
                    return;
                }

                foreach (var deviceObservableModel in selectList)
                {
                    deviceObservableModel.StartAdbCommand((AdbCommandModel)value);
                }
            };
        }

        /// <summary>
        /// 切换全选状态。
        /// </summary>
        private void SelectAll()
        {
            IsSelectAll = !IsSelectAll;
        }

        /// <summary>
        /// 执行 TWRP 命令。
        /// </summary>
        private void TWRPCommand()
        {
            var selectList = GetSelectedDevices();
            if (selectList.Count == 0)
            {
                Growl.Warning("请先选择手机");
                return;
            }

            CommandListViewModel.Show(_deviceList, value =>
            {
                Task.Run(() =>
                {
                    foreach (var deviceObservableModel in selectList)
                    {
                        deviceObservableModel.ExecuteTWRPCommand((CommandModel)value);
                    }
                });
            });
        }

        /// <summary>
        /// 选择更新包并执行系统更新。
        /// </summary>
        private void UpdateSystem()
        {
            var selectList = GetSelectedDevices();
            if (selectList.Count == 0)
            {
                Growl.Warning("请先选择手机");
                return;
            }

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "更新文件|*.zip";
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var deviceObservableModel in selectList)
                    {
                        deviceObservableModel.UpdateSystem(openFileDialog.FileName);
                    }
                }
            }
        }

        /// <summary>
        /// 打开单控投屏窗口。
        /// </summary>
        private void SingleScreen()
        {
            try
            {
                var selectList = GetSelectedDevices();
                if (selectList.Count == 0)
                {
                    Growl.Warning("请先选择手机");
                    return;
                }

                foreach (var deviceObservableModel in selectList)
                {
                    if (deviceObservableModel?.Device == null)
                    {
                        continue;
                    }

                    DeviceScreenViewModel
                        .Show(deviceObservableModel.Device)
                        .StartScreen(AppService.Instance.AppConfig.ControlCastScreenResolution, AppService.Instance.AppConfig.ControlCastScreenRate);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 执行恢复出厂设置。
        /// </summary>
        private void RestoreFactory()
        {
            try
            {
                var selectList = GetSelectedDevices();
                if (selectList.Count == 0)
                {
                    Growl.Warning("请先选择手机");
                    return;
                }

                foreach (var deviceObservableModel in selectList)
                {
                    if (deviceObservableModel?.Device == null)
                    {
                        continue;
                    }

                    deviceObservableModel.RestoreFactory();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 根据关键字和筛选项过滤设备列表。
        /// </summary>
        private bool FilterDevice(object obj)
        {
            if (!(obj is DeviceObservableModel deviceObservableModel))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var keyword = SearchKeyword.Trim();
                var isKeywordMatched =
                    ContainsKeyword(deviceObservableModel.Serial, keyword) ||
                    ContainsKeyword(deviceObservableModel.Brand, keyword) ||
                    ContainsKeyword(deviceObservableModel.ModelName, keyword) ||
                    ContainsKeyword(deviceObservableModel.Model, keyword) ||
                    ContainsKeyword(deviceObservableModel.AndroidVersion, keyword) ||
                    ContainsKeyword(deviceObservableModel.SystemVersion, keyword) ||
                    ContainsKeyword(deviceObservableModel.DeviceState, keyword) ||
                    ContainsKeyword(deviceObservableModel.TaskDetailDisplayMessage, keyword);

                if (!isKeywordMatched)
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(ConnectionTypeFilter) &&
                !ConnectionTypeFilter.Equals("全部连接", StringComparison.Ordinal) &&
                !deviceObservableModel.ConnectionType.Equals(ConnectionTypeFilter, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ModelFilter) &&
                !ModelFilter.Equals("全部型号", StringComparison.Ordinal) &&
                !deviceObservableModel.ModelName.Equals(ModelFilter, StringComparison.OrdinalIgnoreCase) &&
                !deviceObservableModel.Model.Equals(ModelFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(DeviceStateFilter) &&
                !DeviceStateFilter.Equals("全部状态", StringComparison.Ordinal) &&
                !deviceObservableModel.DeviceState.Equals(DeviceStateFilter, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 打开群控投屏窗口。
        /// </summary>
        private void AllScreen()
        {
            DeviceScreenWindowModel.Show();
        }

        /// <summary>
        /// 打开文件同步窗口。
        /// </summary>
        private void SyncFile()
        {
            var selectedDevices = GetSelectedDevices();
            var devices = selectedDevices
                .Where(item => item.Device != null)
                .Select(item => item.Device)
                .ToList();

            if (devices.Count == 0)
            {
                Growl.Error("请先选择手机");
                return;
            }

            var viewModel = new SyncFileViewModel
            {
                NeedInstallDeviceList = devices
            };
            var dialog = new SyncFileView
            {
                DataContext = viewModel
            };
            viewModel.Dialog = Dialog.Show(dialog);
        }

        /// <summary>
        /// 刷新设备视图和汇总信息。
        /// </summary>
        private void RefreshDeviceView()
        {
            DeviceView.Refresh();
            RefreshDeviceSummary();
            RefreshConnectDeviceNameList();
        }

        /// <summary>
        /// 切换侧边栏显示状态。
        /// </summary>
        private void ToggleSidebar()
        {
            IsSidebarExpanded = !IsSidebarExpanded;
        }

        /// <summary>
        /// 获取当前选中的设备列表。
        /// </summary>
        private List<DeviceObservableModel> GetSelectedDevices()
        {
            return _deviceList.FindAll(item => item != null && item.IsSelect);
        }

        /// <summary>
        /// 刷新设备展示层的衍生状态。
        /// </summary>
        private void RefreshDevicePresentationState()
        {
            for (var index = 0; index < _deviceList.Count; index++)
            {
                _deviceList[index].DisplayIndex = index + 1;
            }

            ListCount = _deviceList.Count;
            RefreshDeviceSummary();
            RefreshConnectDeviceNameList();
            RefreshModelFilterList();
            DeviceView.Refresh();
            RaisePageStateChanged();
        }

        /// <summary>
        /// 刷新设备汇总文案。
        /// </summary>
        private void RefreshDeviceSummary()
        {
            var systemCount = _deviceList.Count(item => item.Device.State == DeviceState.Online);
            var recoveryCount = _deviceList.Count(item => item.Device.State == DeviceState.Recovery);
            var sideloadCount = _deviceList.Count(item => item.Device.State == DeviceState.Sideload);
            var downloadCount = _deviceList.Count(item => item.Device.State == DeviceState.BootLoader);
            DeviceSummaryText = $"共 {_deviceList.Count} 台设备 · 系统 {systemCount} 台 · 恢复 {recoveryCount} 台 · 侧载 {sideloadCount} 台 · Download {downloadCount} 台";
        }

        /// <summary>
        /// 刷新已连接设备名称筛选项，保留旧数据结构兼容。
        /// </summary>
        private void RefreshConnectDeviceNameList()
        {
            ConnectDeviceNameList.Clear();
            ConnectDeviceNameList.Add("全部");

            foreach (var deviceName in _deviceList
                         .Select(item => item.Device.Name)
                         .Where(item => !string.IsNullOrWhiteSpace(item))
                         .Distinct()
                         .OrderBy(item => item))
            {
                ConnectDeviceNameList.Add(deviceName);
            }
        }

        /// <summary>
        /// 刷新型号筛选项。
        /// </summary>
        private void RefreshModelFilterList()
        {
            var currentFilter = ModelFilter;
            ModelFilterList.Clear();
            ModelFilterList.Add("全部型号");

            foreach (var modelName in _deviceList
                         .Select(item => string.IsNullOrWhiteSpace(item.ModelName) ? item.Model : item.ModelName)
                         .Where(item => !string.IsNullOrWhiteSpace(item))
                         .Distinct()
                         .OrderBy(item => item))
            {
                ModelFilterList.Add(modelName);
            }

            if (ModelFilterList.Contains(currentFilter))
            {
                ModelFilter = currentFilter;
                return;
            }

            ModelFilter = "全部型号";
        }

        /// <summary>
        /// 初始化固定筛选项。
        /// </summary>
        private void InitializeStaticFilters()
        {
            ModelFilterList.Add("全部型号");

            ConnectionTypeList.Add("全部连接");
            ConnectionTypeList.Add("USB");
            ConnectionTypeList.Add("以太网");

            DeviceStateList.Add("全部状态");
            DeviceStateList.Add("系统");
            DeviceStateList.Add("恢复");
            DeviceStateList.Add("Download");
            DeviceStateList.Add("侧载");
            DeviceStateList.Add("离线");
            DeviceStateList.Add("验证中");
            DeviceStateList.Add("无权限");
            DeviceStateList.Add("未验证");
            DeviceStateList.Add("网络模式");
            DeviceStateList.Add("未知状态");
        }

        /// <summary>
        /// 初始化标题和版本文案。
        /// </summary>
        private void InitializeTitle()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
                var shortVersion = version.Substring(0, Math.Max(version.Length - 2, 1));
                Title = "极星刷机工具 " + shortVersion;
                AppVersionText = "版本号：" + shortVersion;
            }
            catch
            {
                Title = "极星刷机工具";
            }
        }

        /// <summary>
        /// 刷新页面状态相关属性通知。
        /// </summary>
        private void RaisePageStateChanged()
        {
            OnPropertyChanged(nameof(HasDevices));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowDeviceTable));
        }

        /// <summary>
        /// 切回 UI 线程执行更新。
        /// </summary>
        private void RunOnUiThread(Action action)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                action();
                return;
            }

            if (dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action);
        }

        /// <summary>
        /// 判断指定文本是否包含关键字。
        /// </summary>
        private static bool ContainsKeyword(string sourceText, string keyword)
        {
            return !string.IsNullOrWhiteSpace(sourceText) &&
                   sourceText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 设备集合变化后刷新页面状态。
        /// </summary>
        private void OnDeviceCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePageStateChanged();
        }

        /// <summary>
        /// 处理设备连接事件。
        /// </summary>
        protected virtual void PDeviceConnected(object sender, DeviceEventArgs e)
        {
            if (e.DeviceModel == null)
            {
                return;
            }

            AddDevice(e.DeviceModel, e.Index);
        }

        /// <summary>
        /// 处理设备断开事件。
        /// </summary>
        protected virtual void PDeviceDisconnected(object sender, DeviceEventArgs e)
        {
            if (e.DeviceModel == null)
            {
                return;
            }

            RemoveDevice(e.DeviceModel);
        }

        /// <summary>
        /// 处理设备状态变化事件。
        /// </summary>
        protected virtual void PDeviceChanage(object sender, DeviceEventArgs e)
        {
            if (e.DeviceModel == null)
            {
                return;
            }

            if (e.DeviceModel.State == DeviceState.Offline)
            {
                RemoveDevice(e.DeviceModel);
            }
        }

        /// <summary>
        /// 添加设备到页面集合。
        /// </summary>
        protected virtual void AddDevice(DeviceModel device, int index)
        {
            var deviceObservableModel = new DeviceObservableModel(device);

            RunOnUiThread(() =>
            {
                try
                {
                    var safeIndex = Math.Max(0, Math.Min(index, _deviceList.Count));
                    _deviceList.Insert(safeIndex, deviceObservableModel);
                    DeviceCollection.Insert(safeIndex, deviceObservableModel);
                }
                catch
                {
                    _deviceList.Add(deviceObservableModel);
                    DeviceCollection.Add(deviceObservableModel);
                }

                deviceObservableModel.IsSelect = _isSelectAll;
                RefreshDevicePresentationState();
            });
        }

        /// <summary>
        /// 从页面集合移除设备。
        /// </summary>
        protected virtual void RemoveDevice(DeviceModel device)
        {
            var deviceObservableModel = _deviceList.Find(item => item != null && item.Device.RoSerialNo.Equals(device.RoSerialNo));
            if (deviceObservableModel == null)
            {
                return;
            }

            RunOnUiThread(() =>
            {
                DeviceCollection.Remove(deviceObservableModel);
                _deviceList.Remove(deviceObservableModel);
                RefreshDevicePresentationState();
            });
        }
    }
}

