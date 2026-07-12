using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.EventArg;
using JiXingFlashTool.Model;
using JiXingFlashTool.Models;
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.ViewModels.AllScreen;
using JiXingFlashTool.ViewModels.Odin;
using JiXingFlashTool.Views;
using JiXingFlashTool.Enums;
using JiXingFlashTool.Model.Payload;
using JiXingFlashTool.Tasks;
using JXAdbCore.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using LanguageCore;
using TaskCore.Scheduling;
using TaskCore.Sessions;
using TaskCore.Tasks;
using JiXingFlashTool.Utils;

namespace JiXingFlashTool.ViewModels
{
    /// <summary>
    /// 主页面视图模型，负责设备列表状态、筛选条件和主窗口入口命令的统一编排。
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        #region 状态与集合

        /// <summary>
        /// 主窗口视图模型单例，供任务和服务层按需回查当前设备列表。
        /// </summary>
        public static MainWindowViewModel Instance { get; private set; }

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
        [NotifyPropertyChangedFor(nameof(ShowDeviceTable))]
        private bool _isLoading;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
        [NotifyPropertyChangedFor(nameof(ShowDeviceTable))]
        private bool _hasLoadError;

        [ObservableProperty]
        private string _loadErrorMessage;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _modelFilter;

        [ObservableProperty]
        private string _connectionTypeFilter;

        [ObservableProperty]
        private string _deviceStateFilter;

        [ObservableProperty]
        private string _deviceSummaryText;

        [ObservableProperty]
        private string _appVersionText;

        [ObservableProperty]
        private bool _isSelectAll;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SidebarPanelWidth))]
        [NotifyPropertyChangedFor(nameof(SidebarHeaderHeight))]
        [NotifyPropertyChangedFor(nameof(SidebarFooterHeight))]
        [NotifyPropertyChangedFor(nameof(SidebarToggleIconGlyph))]
        private bool _isSidebarExpanded = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaintainerModeEntryText))]
        private bool _isProfessionalModeEnabled;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDeviceManagePage))]
        [NotifyPropertyChangedFor(nameof(DeviceManageNavBackground))]
        [NotifyPropertyChangedFor(nameof(DeviceManageNavForeground))]
        [NotifyPropertyChangedFor(nameof(OdinNavBackground))]
        [NotifyPropertyChangedFor(nameof(OdinNavForeground))]
        private bool _isOdinFlashPage;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SidebarDeviceCountText))]
        private int _listCount;
        private readonly RelayCommand _showDeviceManagePageCommand;
        private readonly AsyncRelayCommand _showOdinFlashPageCommand;
        private readonly RelayCommand _showResourcePageCommand;
        private const double SidebarWidthScale = 1D;
        private const double SidebarCollapsedWidth = 60D;

        /// <summary>
        /// 设备列表源集合。
        /// </summary>
        public ObservableCollection<DeviceItemViewModel> DeviceCollection { get; } = new ObservableCollection<DeviceItemViewModel>();

        /// <summary>
        /// 设备列表可见集合，用于承载当前筛选后的展示行。
        /// </summary>
        public ObservableCollection<DeviceItemViewModel> DeviceView { get; } = new ObservableCollection<DeviceItemViewModel>();

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
        /// Odin 刷机页面视图模型，负责右侧 Odin 刷机内容区的数据和命令。
        /// </summary>
        public OdinFlashViewModel OdinFlashViewModel { get; } = new OdinFlashViewModel();

        /// <summary>
        /// 侧边栏维护者模式入口文案。
        /// </summary>
        public string MaintainerModeEntryText => IsProfessionalModeEnabled ? GetLangText("Sidebar_ExitMaintainerMode") : GetLangText("Sidebar_MaintainerMode");

        /// <summary>
        /// 当前右侧内容区是否显示设备管理页面。
        /// </summary>
        public bool IsDeviceManagePage => !IsOdinFlashPage;

        /// <summary>
        /// 设备管理菜单背景色。
        /// </summary>
        public string DeviceManageNavBackground => IsDeviceManagePage ? "#EBF0FF" : "Transparent";

        /// <summary>
        /// 设备管理菜单前景色。
        /// </summary>
        public string DeviceManageNavForeground => IsDeviceManagePage ? "#5C82FD" : "#B3333333";

        /// <summary>
        /// Odin 刷机菜单背景色。
        /// </summary>
        public string OdinNavBackground => IsOdinFlashPage ? "#EBF0FF" : "Transparent";

        /// <summary>
        /// Odin 刷机菜单前景色。
        /// </summary>
        public string OdinNavForeground => IsOdinFlashPage ? "#5C82FD" : "#B3333333";

        /// <summary>
        /// 侧边栏当前宽度。
        /// </summary>
        public double SidebarPanelWidth => IsSidebarExpanded ? 223D * SidebarWidthScale : SidebarCollapsedWidth;

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
        /// 侧边栏设备统计文案。
        /// </summary>
        public string SidebarDeviceCountText => string.Format(GetLangText("Sidebar_DeviceCount"), ListCount);

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

        /// <summary>
        /// 搜索关键字变化后启动筛选防抖。
        /// </summary>
        partial void OnSearchKeywordChanged(string value)
        {
            ScheduleRefreshDeviceView();
        }

        /// <summary>
        /// 筛选条件变化后刷新可见设备集合。
        /// </summary>
        partial void OnModelFilterChanged(string value)
        {
            RefreshDeviceView();
        }

        /// <summary>
        /// 连接方式筛选条件变化后刷新可见设备集合。
        /// </summary>
        partial void OnConnectionTypeFilterChanged(string value)
        {
            RefreshDeviceView();
        }

        /// <summary>
        /// 设备状态筛选条件变化后刷新可见设备集合。
        /// </summary>
        partial void OnDeviceStateFilterChanged(string value)
        {
            RefreshDeviceView();
        }

        /// <summary>
        /// 全选状态变化后同步到当前筛选结果中的设备行。
        /// </summary>
        partial void OnIsSelectAllChanged(bool value)
        {
            foreach (var deviceItemViewModel in DeviceView)
            {
                deviceItemViewModel.IsSelect = value;
            }
        }

        private readonly List<DeviceItemViewModel> _deviceList = new List<DeviceItemViewModel>();
        private readonly DispatcherTimer _searchRefreshTimer;

        #endregion

        #region 命令与生命周期

        /// <summary>
        /// 全局设备任务调度器。
        /// </summary>
        private IDeviceTaskScheduler TaskScheduler => App.Instance.TaskScheduler;

        /// <summary>
        /// 全选命令。
        /// </summary>TaskScheduler
        public RelayCommand SelectAllCommand => new Lazy<RelayCommand>(() => new RelayCommand(SelectAll)).Value;

        /// <summary>
        /// 打开常用 ADB 命令窗口。
        /// </summary>
        public RelayCommand SystemShowViewCommand => new Lazy<RelayCommand>(() => new RelayCommand(ShowAdbCommandView)).Value;

        /// <summary>
        /// 打开维护者模式登录弹窗或退出维护者模式。
        /// </summary>
        public RelayCommand ProfessionalModeCommand => new Lazy<RelayCommand>(() => new RelayCommand(OpenProfessionalModeDialog)).Value;

        /// <summary>
        /// 打开系统更新窗口。
        /// </summary>
        public RelayCommand UpdateSystemCommand => new Lazy<RelayCommand>(() => new RelayCommand(UpdateSystem)).Value;

        /// <summary>
        /// 执行专业指令按钮组中的快捷操作。
        /// </summary>
        public RelayCommand<string> ProfessionalInstructionCommand => new Lazy<RelayCommand<string>>(() => new RelayCommand<string>(ExecuteProfessionalInstructionCommand)).Value;

        /// <summary>
        /// 执行维护指令按钮组中的快捷操作。
        /// </summary>
        public RelayCommand<string> MaintenanceInstructionCommand => new Lazy<RelayCommand<string>>(() => new RelayCommand<string>(ExecuteMaintenanceInstructionCommand)).Value;

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
        /// 切换中文与英文界面语言命令。
        /// </summary>
        public RelayCommand ToggleLanguageCommand => new Lazy<RelayCommand>(() => new RelayCommand(ToggleLanguage)).Value;

        /// <summary>
        /// 显示设备管理页面命令。
        /// </summary>
        public RelayCommand ShowDeviceManagePageCommand => _showDeviceManagePageCommand;

        /// <summary>
        /// 显示 Odin 刷机页面命令。
        /// </summary>
        public AsyncRelayCommand ShowOdinFlashPageCommand => _showOdinFlashPageCommand;

        /// <summary>
        /// 显示资源管理弹窗命令。
        /// </summary>
        public RelayCommand ShowResourcePageCommand => _showResourcePageCommand;

        /// <summary>
        /// 初始化主页面视图模型。
        /// </summary>
        public MainWindowViewModel()
        {
            Instance = this;
            _showDeviceManagePageCommand = new RelayCommand(ShowDeviceManagePage);
            _showOdinFlashPageCommand = new AsyncRelayCommand(ShowOdinFlashPageAsync);
            _showResourcePageCommand = new RelayCommand(ShowResourcePage);
            WeakEventManager<LocalizationService, EventArgs>.AddHandler(
                LocalizationService.Instance,
                nameof(LocalizationService.LanguageChanged),
                OnLanguageChanged);
            InitializeStaticFilters();
            InitializeTitle();
            LoadErrorMessage = GetLangText("Message_DeviceListInitFailed");

            DeviceCollection.CollectionChanged += OnDeviceCollectionChanged;
            _searchRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(160)
            };
            _searchRefreshTimer.Tick += OnSearchRefreshTimerTick;

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
            LoadErrorMessage = GetLangText("Message_DeviceListInitFailed");

            _ = Task.Run(() =>
            {
                try
                {
                    AppService.Instance.Read();
                    DeviceService.Instance.Init();
                    AdbService.Instance.StartServiceAsync();
                    CastScreenManageService.Instance.StartMonitor();

                    CommonTool.RunOnUiThread(() =>
                    {
                        IsLoading = false;
                        RefreshDeviceSummary();
                    });
                }
                catch (Exception ex)
                {
                    CommonTool.RunOnUiThread(() =>
                    {
                        IsLoading = false;
                        HasLoadError = true;
                        LoadErrorMessage = GetLangText("Message_DeviceServiceInitFailed");
                        Growl.Error(ex.Message);
                    });
                }
            });
        }

        #endregion

        #region 页面操作

        /// <summary>
        /// 打开常用 ADB 命令窗口。
        /// </summary>
        private void ShowAdbCommandView()
        {
            var selectList = GetSelectedDevices();
            if (selectList.Count == 0)
            {
                Growl.Warning(GetLangText("Message_SelectPhoneFirst"));
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
            viewModel.Dialog = Dialog.Show(dialog);
            viewModel.FinishDelegate = value =>
            {
                if (value == null)
                {
                    return;
                }

                foreach (var deviceItemViewModel in selectList)
                {
                    deviceItemViewModel.StartAdbCommand((AdbCommandModel)value);
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
        /// 选择更新包并执行系统更新。
        /// </summary>
        private void UpdateSystem()
        {
            var selectList = GetSelectedDevices();
            if (selectList.Count == 0)
            {
                Growl.Warning(GetLangText("Message_SelectPhoneFirst"));
                return;
            }

            var viewModel = new UpdateSystemDialogViewModel(selectList, EnqueueUpdateTask);
            var dialog = new UpdateSystemDialogView
            {
                DataContext = viewModel
            };

            viewModel.Dialog = Dialog.Show(dialog);
        }

        /// <summary>
        /// 打开维护者模式登录弹窗或退出维护者模式。
        /// </summary>
        private void OpenProfessionalModeDialog()
        {
            if (IsProfessionalModeEnabled)
            {
                ExitProfessionalMode();
                return;
            }

            var viewModel = new LoginDialogViewModel(EnableProfessionalMode);
            var dialog = new LoginDialogView
            {
                DataContext = viewModel
            };

            viewModel.Dialog = Dialog.Show(dialog);
        }

        /// <summary>
        /// 进入维护者模式并刷新入口文案。
        /// </summary>
        private void EnableProfessionalMode()
        {
            IsProfessionalModeEnabled = true;
            Growl.Success(GetLangText("Message_EnterMaintainerMode"));
        }

        /// <summary>
        /// 退出维护者模式并刷新入口文案。
        /// </summary>
        private void ExitProfessionalMode()
        {
            IsProfessionalModeEnabled = false;
            Growl.Info(GetLangText("Message_ExitMaintainerMode"));
        }

        /// <summary>
        /// 维护指令按钮的占位处理，仅保留 UI 绑定，不执行业务逻辑。
        /// </summary>
        /// <param name="commandKey">指令标识。</param>
        private void ExecuteMaintenanceInstructionCommand(string commandKey)
        {
            var selectList = GetSelectedDevices().Where(item => item?.Device != null).ToList();
            if (selectList.Count == 0)
            {
                Growl.Warning(GetLangText("Message_SelectPhoneFirst"));
                return;
            }

            switch (commandKey)
            {
                case "RebootDevice":
                    EnqueueSingleCommand(selectList, CommandType.RebootSystem);
                    break;
                case "FlashOn":
                    EnqueueSingleCommand(selectList, CommandType.OpenFlashlight);
                    break;
                case "FlashOff":
                    EnqueueSingleCommand(selectList, CommandType.CloseFlashlight);
                    break;
                case "ShellCommand":
                    ShowAdbCommandView();
                    break;
                case "RootDevice":
                    EnqueueSingleCommand(selectList, CommandType.FlashMagisk);
                    break;
                case "CheckRootDevice":
                    EnqueueSingleCommand(selectList, CommandType.FlashMagisk);
                    break;
                case "CheckSystemUpdate":
                    UpdateSystem();
                    break;
            }
        }

        /// <summary>
        /// 为需要文件的专业指令选择文件后再执行。
        /// </summary>
        /// <param name="selectList">当前选中的设备列表。</param>
        /// <param name="fileFilter">文件筛选器。</param>
        /// <param name="action">文件执行逻辑。</param>
        private static void ExecuteProfessionalFileCommand(List<DeviceItemViewModel> selectList, string fileFilter, Action<DeviceItemViewModel, string> action)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = string.IsNullOrWhiteSpace(fileFilter) ? GetLangText("FileFilter_All") : fileFilter;
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                var filePath = openFileDialog.FileName;
                Task.Run(() =>
                {
                    foreach (var deviceItemViewModel in selectList)
                    {
                        action(deviceItemViewModel, filePath);
                    }
                });
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
                    Growl.Warning(GetLangText("Message_SelectPhoneFirst"));
                    return;
                }

                foreach (var deviceItemViewModel in selectList)
                {
                    if (deviceItemViewModel?.Device == null)
                    {
                        continue;
                    }

                    DeviceScreenViewModel
                        .Show(deviceItemViewModel.Device)
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
                    Growl.Warning(GetLangText("Message_SelectPhoneFirst"));
                    return;
                }

                foreach (var deviceItemViewModel in selectList)
                {
                    if (deviceItemViewModel?.Device == null)
                    {
                        continue;
                    }

                    deviceItemViewModel.RestoreFactory();
                }
            }
            catch
            {
            }
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
                Growl.Error(GetLangText("Message_SelectPhoneFirst"));
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

        #endregion

        #region 筛选与排序

        /// <summary>
        /// 刷新设备视图和汇总信息。
        /// </summary>
        private void RefreshDeviceView()
        {
            SyncDeviceView();
            RefreshDeviceSummary();
            RefreshConnectDeviceNameList();
        }

        /// <summary>
        /// 延迟刷新搜索结果，避免每次输入都立刻重建列表。
        /// </summary>
        private void ScheduleRefreshDeviceView()
        {
            _searchRefreshTimer.Stop();
            _searchRefreshTimer.Start();
        }

        /// <summary>
        /// 搜索防抖定时器触发时刷新可见集合。
        /// </summary>
        private void OnSearchRefreshTimerTick(object sender, EventArgs e)
        {
            _searchRefreshTimer.Stop();
            RefreshDeviceView();
        }

        /// <summary>
        /// 判断设备是否满足当前筛选条件。
        /// </summary>
        private bool IsDeviceVisible(DeviceItemViewModel deviceItemViewModel)
        {
            if (deviceItemViewModel == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var keyword = SearchKeyword.Trim();
                var isKeywordMatched =
                    ContainsKeyword(deviceItemViewModel.Serial, keyword) ||
                    ContainsKeyword(deviceItemViewModel.Brand, keyword) ||
                    ContainsKeyword(deviceItemViewModel.ModelName, keyword) ||
                    ContainsKeyword(deviceItemViewModel.Model, keyword) ||
                    ContainsKeyword(deviceItemViewModel.AndroidVersion, keyword) ||
                    ContainsKeyword(deviceItemViewModel.SystemVersion, keyword) ||
                    ContainsKeyword(deviceItemViewModel.DeviceState, keyword) ||
                    ContainsKeyword(deviceItemViewModel.TaskDetailDisplayMessage, keyword);

                if (!isKeywordMatched)
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(ConnectionTypeFilter) &&
                !ConnectionTypeFilter.Equals(GetLangText("Filter_AllConnection"), StringComparison.Ordinal) &&
                !deviceItemViewModel.ConnectionType.Equals(ConnectionTypeFilter, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ModelFilter) &&
                !ModelFilter.Equals(GetLangText("Filter_AllModel"), StringComparison.Ordinal) &&
                !deviceItemViewModel.ModelName.Equals(ModelFilter, StringComparison.OrdinalIgnoreCase) &&
                !deviceItemViewModel.Model.Equals(ModelFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(DeviceStateFilter) &&
                !DeviceStateFilter.Equals(GetLangText("Filter_AllState"), StringComparison.Ordinal) &&
                !deviceItemViewModel.DeviceState.Equals(DeviceStateFilter, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 同步当前可见设备集合，避免依赖 ICollectionView.Refresh。
        /// </summary>
        private void SyncDeviceView()
        {
            CommonTool.RunOnUiThread(() =>
            {
                var filteredDevices = _deviceList
                    .Where(IsDeviceVisible)
                    .OrderBy(item => item, DeviceComparer)
                    .ToList();
                DeviceView.Clear();

                for (var index = 0; index < filteredDevices.Count; index++)
                {
                    filteredDevices[index].DisplayIndex = index + 1;
                    DeviceView.Add(filteredDevices[index]);
                }

                RaisePageStateChanged();
            });
        }

        /// <summary>
        /// 将单个设备插入可见集合中，保持与源集合一致的顺序。
        /// </summary>
        /// <param name="deviceItemViewModel">待插入的设备项。</param>
        /// <param name="sourceIndex">源集合中的位置。</param>
        private void InsertDeviceViewItem(DeviceItemViewModel deviceItemViewModel, int sourceIndex)
        {
            if (!IsDeviceVisible(deviceItemViewModel))
            {
                return;
            }

            var insertIndex = 0;
            for (; insertIndex < DeviceView.Count; insertIndex++)
            {
                if (DeviceComparer.Compare(deviceItemViewModel, DeviceView[insertIndex]) < 0)
                {
                    break;
                }
            }

            if (insertIndex < 0 || insertIndex > DeviceView.Count)
            {
                insertIndex = DeviceView.Count;
            }

            DeviceView.Insert(insertIndex, deviceItemViewModel);
            RefreshVisibleDisplayIndex();
        }

        /// <summary>
        /// 刷新当前可见设备的序号，避免表格序号错乱。
        /// </summary>
        private void RefreshVisibleDisplayIndex()
        {
            for (var index = 0; index < DeviceView.Count; index++)
            {
                DeviceView[index].DisplayIndex = index + 1;
            }
        }

        /// <summary>
        /// 设备排序比较器，USB 按序列号排序，以太网按 IP 数值排序。
        /// </summary>
        private static readonly IComparer<DeviceItemViewModel> DeviceComparer = Comparer<DeviceItemViewModel>.Create(CompareDeviceItems);

        /// <summary>
        /// 比较两个设备项的排序位置。
        /// </summary>
        /// <param name="left">左侧设备项。</param>
        /// <param name="right">右侧设备项。</param>
        /// <returns>排序结果。</returns>
        private static int CompareDeviceItems(DeviceItemViewModel left, DeviceItemViewModel right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            var leftIsEthernet = IsEthernetDevice(left);
            var rightIsEthernet = IsEthernetDevice(right);

            if (leftIsEthernet != rightIsEthernet)
            {
                return leftIsEthernet ? 1 : -1;
            }

            if (leftIsEthernet)
            {
                var ipCompare = CompareEthernetSerial(left.Serial, right.Serial);
                if (ipCompare != 0)
                {
                    return ipCompare;
                }
            }
            else
            {
                var serialCompare = StringComparer.OrdinalIgnoreCase.Compare(left.Serial ?? string.Empty, right.Serial ?? string.Empty);
                if (serialCompare != 0)
                {
                    return serialCompare;
                }
            }

            return StringComparer.OrdinalIgnoreCase.Compare(left.Device?.Name ?? string.Empty, right.Device?.Name ?? string.Empty);
        }

        /// <summary>
        /// 判断设备是否为以太网连接。
        /// </summary>
        /// <param name="deviceItemViewModel">设备项。</param>
        /// <returns>是否为以太网设备。</returns>
        private static bool IsEthernetDevice(DeviceItemViewModel deviceItemViewModel)
        {
            return !string.IsNullOrWhiteSpace(deviceItemViewModel?.Serial) &&
                   deviceItemViewModel.Serial.Contains(":");
        }

        /// <summary>
        /// 按以太网 IP 地址排序。
        /// </summary>
        /// <param name="leftSerial">左侧序列号。</param>
        /// <param name="rightSerial">右侧序列号。</param>
        /// <returns>排序结果。</returns>
        private static int CompareEthernetSerial(string leftSerial, string rightSerial)
        {
            var leftHost = GetEthernetHost(leftSerial);
            var rightHost = GetEthernetHost(rightSerial);

            var leftBytes = TryGetIpBytes(leftHost);
            var rightBytes = TryGetIpBytes(rightHost);

            if (leftBytes != null && rightBytes != null)
            {
                var length = Math.Min(leftBytes.Length, rightBytes.Length);
                for (var index = 0; index < length; index++)
                {
                    var compare = leftBytes[index].CompareTo(rightBytes[index]);
                    if (compare != 0)
                    {
                        return compare;
                    }
                }

                var lengthCompare = leftBytes.Length.CompareTo(rightBytes.Length);
                if (lengthCompare != 0)
                {
                    return lengthCompare;
                }
            }

            return StringComparer.OrdinalIgnoreCase.Compare(leftSerial ?? string.Empty, rightSerial ?? string.Empty);
        }

        /// <summary>
        /// 提取以太网设备的主机部分。
        /// </summary>
        /// <param name="serial">ADB 序列号。</param>
        /// <returns>主机地址。</returns>
        private static string GetEthernetHost(string serial)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                return string.Empty;
            }

            var colonIndex = serial.IndexOf(':');
            return colonIndex > 0 ? serial.Substring(0, colonIndex) : serial;
        }

        /// <summary>
        /// 解析 IP 字节数组，失败时返回 null。
        /// </summary>
        /// <param name="host">主机地址。</param>
        /// <returns>IP 字节数组。</returns>
        private static byte[] TryGetIpBytes(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return null;
            }

            if (!IPAddress.TryParse(host, out var ipAddress))
            {
                return null;
            }

            return ipAddress.GetAddressBytes();
        }

        #endregion

        #region 导航

        /// <summary>
        /// 切换侧边栏显示状态。
        /// </summary>
        private void ToggleSidebar()
        {
            IsSidebarExpanded = !IsSidebarExpanded;
        }

        /// <summary>
        /// 关闭 Odin 页面覆盖层，恢复显示原设备管理内容。
        /// </summary>
        private void ShowDeviceManagePage()
        {
            IsOdinFlashPage = false;
        }

        /// <summary>
        /// 将右侧内容区切换到 Odin 刷机页面，并刷新 Download 模式设备列表。
        /// </summary>
        /// <returns>异步刷新任务。</returns>
        private async Task ShowOdinFlashPageAsync()
        {
            IsOdinFlashPage = true;
            await OdinFlashViewModel.RefreshDevicesAsync();
        }

        /// <summary>
        /// 打开资源管理弹窗。
        /// </summary>
        private void ShowResourcePage()
        {
            var viewModel = new ResourceManagementViewModel();
            var dialog = new ResourceManagementView
            {
                DataContext = viewModel
            };

            viewModel.Dialog = Dialog.Show(dialog);
        }

        #endregion

        #region 设备展示与本地化

        /// <summary>
        /// 获取当前选中的设备列表。
        /// </summary>
        private List<DeviceItemViewModel> GetSelectedDevices()
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
            RefreshVisibleDisplayIndex();
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
            DeviceSummaryText = string.Format(GetLangText("DeviceSummary_Text"), _deviceList.Count, systemCount, recoveryCount, sideloadCount, downloadCount);
        }

        /// <summary>
        /// 刷新已连接设备名称筛选项，保留旧数据结构兼容。
        /// </summary>
        private void RefreshConnectDeviceNameList()
        {
            ConnectDeviceNameList.Clear();
            ConnectDeviceNameList.Add(GetLangText("Filter_All"));

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
            ModelFilterList.Add(GetLangText("Filter_AllModel"));

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

            ModelFilter = GetLangText("Filter_AllModel");
        }

        /// <summary>
        /// 初始化固定筛选项。
        /// </summary>
        private void InitializeStaticFilters()
        {
            ModelFilterList.Clear();
            ConnectionTypeList.Clear();
            DeviceStateList.Clear();

            ModelFilterList.Add(GetLangText("Filter_AllModel"));

            ConnectionTypeList.Add(GetLangText("Filter_AllConnection"));
            ConnectionTypeList.Add("USB");
            ConnectionTypeList.Add(GetLangText("Connection_Ethernet"));

            DeviceStateList.Add(GetLangText("Filter_AllState"));
            DeviceStateList.Add(GetLangText("DeviceState_System"));
            DeviceStateList.Add(GetLangText("DeviceState_Recovery"));
            DeviceStateList.Add("Download");
            DeviceStateList.Add(GetLangText("DeviceState_Sideload"));
            DeviceStateList.Add(GetLangText("DeviceState_Offline"));
            DeviceStateList.Add(GetLangText("DeviceState_Verifying"));
            DeviceStateList.Add(GetLangText("DeviceState_NoPermission"));
            DeviceStateList.Add(GetLangText("DeviceState_Unauthorized"));
            DeviceStateList.Add(GetLangText("DeviceState_NetworkMode"));
            DeviceStateList.Add(GetLangText("DeviceState_Unknown"));
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
                Title = GetLangText("App_Title") + " " + shortVersion;
                AppVersionText = string.Format(GetLangText("Version_Text"), shortVersion);
            }
            catch
            {
                Title = GetLangText("App_Title");
            }
        }

        /// <summary>
        /// 获取当前语言下的界面文案。
        /// </summary>
        /// <param name="key">语言资源键。</param>
        /// <returns>当前语言对应的文案。</returns>
        private static string GetLangText(string key)
        {
            return LocalizationService.Instance.GetString(string.Empty, key);
        }

        /// <summary>
        /// 切换当前界面语言。
        /// </summary>
        private void ToggleLanguage()
        {
            var currentCultureName = LocalizationService.Instance.CurrentCulture?.Name;
            var targetCultureName = string.Equals(currentCultureName, "zh-CN", StringComparison.OrdinalIgnoreCase)
                ? "en"
                : "zh-CN";
            LocalizationService.Instance.ChangeCulture(targetCultureName);
        }

        /// <summary>
        /// 语言变化后刷新 ViewModel 中由代码生成的展示文案。
        /// </summary>
        /// <param name="sender">事件来源。</param>
        /// <param name="e">事件参数。</param>
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            InitializeStaticFilters();
            RefreshModelFilterList();
            RefreshConnectDeviceNameList();
            RefreshDeviceSummary();
            InitializeTitle();
            foreach (var deviceItemViewModel in _deviceList)
            {
                deviceItemViewModel?.RefreshLocalizedText();
            }

            SyncDeviceView();
            OnPropertyChanged(nameof(MaintainerModeEntryText));
            OnPropertyChanged(nameof(SidebarDeviceCountText));
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

        #endregion

        #region 任务分发

        /// <summary>
        /// 执行专业模式快捷指令。
        /// </summary>
        /// <param name="commandKey">指令标识。</param>
        private void ExecuteProfessionalInstructionCommand(string commandKey)
        {
            var selectList = GetSelectedDevices().Where(item => item?.Device != null).ToList();
            if (selectList.Count == 0)
            {
                Growl.Warning(GetLangText("Message_SelectPhoneFirst"));
                return;
            }

            switch (commandKey)
            {
                case "RebootSystem":
                    EnqueueSingleCommand(selectList, CommandType.RebootSystem);
                    break;
                case "RebootTwrp":
                    EnqueueSingleCommand(selectList, CommandType.RebootRecovery);
                    break;
                case "RebootDownload":
                    EnqueueSingleCommand(selectList, CommandType.RebootDownload);
                    break;
                case "Wipe":
                    EnqueueSingleCommand(selectList, CommandType.WipeUserData);
                    break;
                case "ClearSystem":
                    EnqueueSingleCommand(selectList, CommandType.WipeSystem);
                    break;
                case "FormatData":
                    EnqueueSingleCommand(selectList, CommandType.Format);
                    break;
                case "FlashFile":
                    ExecuteProfessionalFileCommand(selectList, GetLangText("FileFilter_FlashPackage"), EnqueueFlashFileTask);
                    break;
                case "FlashKernel":
                    ExecuteProfessionalFileCommand(selectList, GetLangText("FileFilter_All"), EnqueueUpdateBootRecoveryTaskForKernel);
                    break;
                case "UpdateTwrp":
                    ExecuteProfessionalFileCommand(selectList, GetLangText("FileFilter_Img"), EnqueueUpdateBootRecoveryTask);
                    break;
                case "Decrypt":
                    Task.Run(() =>
                    {
                        foreach (var deviceItemViewModel in selectList)
                        {
                            deviceItemViewModel.Service.Decrypt();
                        }
                    });
                    break;
                case "DisableDeveloper":
                    EnqueueSingleCommand(selectList, CommandType.CloseDeveloperMode);
                    break;
                case "SkipGuide":
                    EnqueueSingleCommand(selectList, CommandType.SkipGuide);
                    break;
                case "0":
                    EnqueueSingleCommand(selectList, CommandType.RebootSystem);
                    break;
                case "5":
                    EnqueueSingleCommand(selectList, CommandType.RebootRecovery);
                    break;
                case "6":
                    EnqueueSingleCommand(selectList, CommandType.RebootDownload);
                    break;
            }
        }

        /// <summary>
        /// 将选中的设备批量封装为 TaskCore 单条任务入队。
        /// </summary>
        /// <param name="selectList">选中的设备列表。</param>
        /// <param name="commandType">需要执行的指令类型。</param>
        private void EnqueueSingleCommand(List<DeviceItemViewModel> selectList, CommandType commandType)
        {

            Task.Run(async () =>
            {
                foreach (var deviceItemViewModel in selectList)
                {
                    if (deviceItemViewModel?.Device == null)
                    {
                        continue;
                    }

                    deviceItemViewModel.TaskDetailMessage = string.Empty;

                    var payload = new SingleCommandPayload(deviceItemViewModel.Device, commandType);
                     _ = TaskScheduler.EnqueueAsync(deviceItemViewModel.Device.Serial, new SingleCommandTask(), payload, observer: deviceItemViewModel.CreateTaskObserver(), detail: commandType.ToString());
                }
            });
        }

        /// <summary>
        /// 将选中的设备批量封装为刷入文件任务入队。
        /// </summary>
        /// <param name="deviceItemViewModel">设备项。</param>
        /// <param name="filePath">本地刷入文件路径。</param>
        private void EnqueueFlashFileTask(DeviceItemViewModel deviceItemViewModel, string filePath)
        {
            if (deviceItemViewModel?.Device == null || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var payload = new FlashFilePayload(deviceItemViewModel.Device, filePath);
            _ = TaskScheduler.EnqueueAsync(deviceItemViewModel.Device.Serial, new FlashFileTask(), payload, observer: deviceItemViewModel.CreateTaskObserver(), detail: "FlashFile");
        }

        /// <summary>
        /// 将选中的设备封装为系统更新任务入队，并支持可选 TWRP 镜像。
        /// </summary>
        /// <param name="deviceItemViewModel">设备项。</param>
        /// <param name="filePath">本地更新文件路径。</param>
        /// <param name="wipeData">是否清除用户数据。</param>
        /// <param name="twrpFilePath">可选 TWRP 镜像路径。</param>
        private void EnqueueUpdateTask(DeviceItemViewModel deviceItemViewModel, string filePath, bool wipeData, string twrpFilePath)
        {
            if (deviceItemViewModel?.Device == null || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }



            var payload = new UpdateTaskPayload(deviceItemViewModel.Device, filePath, wipeData, twrpFilePath, NotifyPropertyChanged: deviceItemViewModel.RefreshDeviceProperties);
            _ = TaskScheduler.EnqueueAsync(deviceItemViewModel.Device.Serial, new UpdateTask(), payload, observer: deviceItemViewModel.CreateTaskObserver(), detail: "UpdateTask");
        }

        /// <summary>
        /// 将选中的设备批量封装为 Boot / Recovery 更新任务入队。
        /// </summary>
        /// <param name="deviceItemViewModel">设备项。</param>
        /// <param name="filePath">本地镜像路径。</param>
        private void EnqueueUpdateBootRecoveryTask(DeviceItemViewModel deviceItemViewModel, string filePath)
        {
            if (deviceItemViewModel?.Device == null || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var payload = new UpdateBootRecoveryPayload(deviceItemViewModel.Device, filePath, TWRPCommandType.UpdateTWRP);
            _ = TaskScheduler.EnqueueAsync(deviceItemViewModel.Device.Serial, new UpdateBootRecoveryTask(), payload, observer: deviceItemViewModel.CreateTaskObserver(), detail: "UpdateTwrp");
        }

        /// <summary>
        /// 将选中的设备批量封装为内核更新任务入队。
        /// </summary>
        /// <param name="deviceItemViewModel">设备项。</param>
        /// <param name="filePath">本地镜像路径。</param>
        private void EnqueueUpdateBootRecoveryTaskForKernel(DeviceItemViewModel deviceItemViewModel, string filePath)
        {
            if (deviceItemViewModel?.Device == null || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var payload = new UpdateBootRecoveryPayload(deviceItemViewModel.Device, filePath, TWRPCommandType.FlashKernel);
            _ = TaskScheduler.EnqueueAsync(deviceItemViewModel.Device.Serial, new UpdateBootRecoveryTask(), payload, observer: deviceItemViewModel.CreateTaskObserver(), detail: "FlashKernel");
        }
        #endregion

        #region 设备连接与断开

        /// <summary>
        /// 添加设备到页面集合。
        /// </summary>
        protected virtual void AddDevice(DeviceModel device, int index)
        {
            var deviceItemViewModel = new DeviceItemViewModel(device);

            CommonTool.RunOnUiThread(() =>
            {
                try
                {
                    var safeIndex = Math.Max(0, Math.Min(index, _deviceList.Count));
                    _deviceList.Insert(safeIndex, deviceItemViewModel);
                    DeviceCollection.Insert(safeIndex, deviceItemViewModel);
                    InsertDeviceViewItem(deviceItemViewModel, safeIndex);
                }
                catch
                {
                    _deviceList.Add(deviceItemViewModel);
                    DeviceCollection.Add(deviceItemViewModel);
                    InsertDeviceViewItem(deviceItemViewModel, _deviceList.Count - 1);
                }

                deviceItemViewModel.IsSelect = IsSelectAll;
                RefreshDevicePresentationState();
            });
        }

        /// <summary>
        /// 从页面集合移除设备。
        /// </summary>
        protected virtual void RemoveDevice(DeviceModel device)
        {
            var deviceItemViewModel = _deviceList.Find(item => item != null && item.Device.RoSerialNo.Equals(device.RoSerialNo));
            if (deviceItemViewModel == null)
            {
                return;
            }

            //如果是需要保持连接就不断开
            if (deviceItemViewModel.Device.IsKeepLink) return;

            CommonTool.RunOnUiThread(() =>
            {
                DeviceCollection.Remove(deviceItemViewModel);
                _deviceList.Remove(deviceItemViewModel);
                RemoveDeviceViewItem(deviceItemViewModel);
                RefreshDevicePresentationState();
            });
        }


        /// <summary>
        /// 从可见集合移除单个设备项。
        /// </summary>
        /// <param name="deviceItemViewModel">待移除的设备项。</param>
        private void RemoveDeviceViewItem(DeviceItemViewModel deviceItemViewModel)
        {
            if (deviceItemViewModel == null)
            {
                return;
            }


            if (deviceItemViewModel.Device.IsKeepLink) return;

            if (DeviceView.Contains(deviceItemViewModel))
            {
                DeviceView.Remove(deviceItemViewModel);
                RefreshVisibleDisplayIndex();
            }
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

        #endregion
    }
}


