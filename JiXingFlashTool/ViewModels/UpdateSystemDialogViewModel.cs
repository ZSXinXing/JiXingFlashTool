using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Model;
using JiXingFlashTool.Utils;
using LanguageCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiXingFlashTool.ViewModels
{
    /// <summary>
    /// 更新文件弹窗视图模型，负责手动文件选择和按设备代号自动匹配更新资源。
    /// </summary>
    public class UpdateSystemDialogViewModel : ObservableObject
    {
        private readonly IReadOnlyList<DeviceItemViewModel> _selectedDevices;
        private readonly Action<DeviceItemViewModel, string, bool, string> _startUpdateTask;
        private readonly Action<DeviceItemViewModel, string> _startTwrpTask;
        private readonly Action<DeviceItemViewModel, string> _startFlashFileTask;
        private readonly RelayCommand _selectFileCommand;
        private readonly RelayCommand _selectTwrpFileCommand;
        private readonly RelayCommand _clearTwrpFileCommand;
        private readonly RelayCommand _cancelCommand;
        private readonly AsyncRelayCommand _startTaskCommand;
        private Task _automaticRecognitionTask;
        private string _selectedFilePath = string.Empty;
        private string _selectedTwrpFilePath = string.Empty;
        private int _selectedModeIndex;
        private bool _wipeData;
        private bool _isRecognizing;
        private bool _isFlashRomEnabled = true;
        private bool _isFlashTwrpEnabled;

        /// <summary>
        /// 使用选中的设备初始化弹窗及自动识别列表。
        /// </summary>
        /// <param name="selectedDevices">当前选中的设备列表。</param>
        /// <param name="startUpdateTask">系统更新任务入队回调。</param>
        /// <param name="startTwrpTask">TWRP 更新任务入队回调。</param>
        public UpdateSystemDialogViewModel(
            IReadOnlyList<DeviceItemViewModel> selectedDevices,
            Action<DeviceItemViewModel, string, bool, string> startUpdateTask,
            Action<DeviceItemViewModel, string> startTwrpTask)
            : this(selectedDevices)
        {
            _startUpdateTask = startUpdateTask ?? throw new ArgumentNullException(nameof(startUpdateTask));
            _startTwrpTask = startTwrpTask ?? throw new ArgumentNullException(nameof(startTwrpTask));
        }

        /// <summary>
        /// 使用选中的设备初始化刷入文件弹窗。
        /// </summary>
        /// <param name="selectedDevices">当前选中的设备列表。</param>
        /// <param name="startFlashFileTask">刷入文件任务入队回调。</param>
        public UpdateSystemDialogViewModel(
            IReadOnlyList<DeviceItemViewModel> selectedDevices,
            Action<DeviceItemViewModel, string> startFlashFileTask)
            : this(selectedDevices)
        {
            _startFlashFileTask = startFlashFileTask ?? throw new ArgumentNullException(nameof(startFlashFileTask));
            IsFlashFileMode = true;
        }

        /// <summary>
        /// 初始化弹窗共用的设备集合和命令。
        /// </summary>
        /// <param name="selectedDevices">当前选中的设备列表。</param>
        private UpdateSystemDialogViewModel(IReadOnlyList<DeviceItemViewModel> selectedDevices)
        {
            _selectedDevices = selectedDevices ?? Array.Empty<DeviceItemViewModel>();
            AutomaticDevices = new ObservableCollection<AutomaticUpdateDeviceItemViewModel>(
                _selectedDevices
                    .Where(item => item?.Device != null)
                    .Select(item => new AutomaticUpdateDeviceItemViewModel(item, GetText("UpdateDialog_AutoWaiting"))));
            AutomaticResourceGroups = CreateAutomaticResourceGroups(AutomaticDevices);

            _selectFileCommand = new RelayCommand(SelectFile);
            _selectTwrpFileCommand = new RelayCommand(SelectTwrpFile);
            _clearTwrpFileCommand = new RelayCommand(ClearTwrpFile);
            _cancelCommand = new RelayCommand(Cancel);
            _startTaskCommand = new AsyncRelayCommand(StartTaskAsync, CanStartTask);
        }

        /// <summary>
        /// 当前弹窗是否用于刷入文件任务。
        /// </summary>
        public bool IsFlashFileMode { get; }

        /// <summary>
        /// 当前弹窗标题。
        /// </summary>
        public string DialogTitleText => GetText(IsFlashFileMode ? "Toolbar_FlashFile" : "Toolbar_UpdateFile");

        /// <summary>
        /// 手动模式的文件选择标签。
        /// </summary>
        public string SelectFileLabel => GetText(IsFlashFileMode ? "FlashDialog_SelectFile" : "UpdateDialog_SelectSystem");

        /// <summary>
        /// 当前 HandyControl 弹窗实例。
        /// </summary>
        public Dialog Dialog { get; set; }

        /// <summary>
        /// 自动更新模式下的设备匹配结果列表。
        /// </summary>
        public ObservableCollection<AutomaticUpdateDeviceItemViewModel> AutomaticDevices { get; }

        /// <summary>
        /// 按设备系列汇总后的自动识别资源列表。
        /// </summary>
        public ObservableCollection<AutomaticUpdateResourceGroupViewModel> AutomaticResourceGroups { get; }

        /// <summary>
        /// 当前模式索引，0 表示手动，1 表示自动。
        /// </summary>
        public int SelectedModeIndex
        {
            get => _selectedModeIndex;
            set
            {
                if (SetProperty(ref _selectedModeIndex, value))
                {
                    OnPropertyChanged(nameof(IsAutomaticMode));
                    OnPropertyChanged(nameof(CanStartCurrentMode));
                    _startTaskCommand.NotifyCanExecuteChanged();
                    StartAutomaticRecognitionIfRequired();
                }
            }
        }

        /// <summary>
        /// 当前是否为自动识别模式。
        /// </summary>
        public bool IsAutomaticMode => SelectedModeIndex == 1;

        /// <summary>
        /// 手动选择的更新文件绝对路径。
        /// </summary>
        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                if (SetProperty(ref _selectedFilePath, value))
                {
                    OnPropertyChanged(nameof(SelectedFileName));
                    OnPropertyChanged(nameof(SelectedFileSizeText));
                    OnPropertyChanged(nameof(HasSelectedFile));
                    OnPropertyChanged(nameof(CanStartCurrentMode));
                    _startTaskCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 手动选择的 TWRP 镜像绝对路径。
        /// </summary>
        public string SelectedTwrpFilePath
        {
            get => _selectedTwrpFilePath;
            set
            {
                if (SetProperty(ref _selectedTwrpFilePath, value))
                {
                    OnPropertyChanged(nameof(SelectedTwrpFileName));
                    OnPropertyChanged(nameof(SelectedTwrpFileSizeText));
                    OnPropertyChanged(nameof(HasSelectedTwrpFile));
                }
            }
        }

        /// <summary>
        /// 是否在更新前清除用户数据。
        /// </summary>
        public bool WipeData
        {
            get => _wipeData;
            set => SetProperty(ref _wipeData, value);
        }

        /// <summary>
        /// 自动模式下是否刷入 ROM。
        /// </summary>
        public bool IsFlashRomEnabled
        {
            get => _isFlashRomEnabled;
            set
            {
                if (SetProperty(ref _isFlashRomEnabled, value))
                {
                    UpdateAutomaticResourceSelection();
                }
            }
        }

        /// <summary>
        /// 自动模式下是否同时刷入 TWRP。
        /// </summary>
        public bool IsFlashTwrpEnabled
        {
            get => _isFlashTwrpEnabled;
            set
            {
                if (SetProperty(ref _isFlashTwrpEnabled, value))
                {
                    UpdateAutomaticResourceSelection();
                }
            }
        }

        /// <summary>
        /// 当前是否正在加载已勾选的 ROM 资源。
        /// </summary>
        public bool IsRomLoading => IsRecognizing && IsFlashRomEnabled;

        /// <summary>
        /// 当前是否正在加载已勾选的 TWRP 资源。
        /// </summary>
        public bool IsTwrpLoading => IsRecognizing && IsFlashTwrpEnabled;

        /// <summary>
        /// 当前是否正在读取并提取自动更新资源。
        /// </summary>
        public bool IsRecognizing
        {
            get => _isRecognizing;
            private set
            {
                if (SetProperty(ref _isRecognizing, value))
                {
                    OnPropertyChanged(nameof(IsRomLoading));
                    OnPropertyChanged(nameof(IsTwrpLoading));
                    OnPropertyChanged(nameof(CanStartCurrentMode));
                    _startTaskCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 手动更新文件的展示名称。
        /// </summary>
        public string SelectedFileName => string.IsNullOrWhiteSpace(SelectedFilePath)
            ? GetText(IsFlashFileMode ? "FlashDialog_FileNotSelected" : "UpdateDialog_SystemNotSelected")
            : Path.GetFileName(SelectedFilePath);

        /// <summary>
        /// 手动 TWRP 文件的展示名称。
        /// </summary>
        public string SelectedTwrpFileName => string.IsNullOrWhiteSpace(SelectedTwrpFilePath)
            ? GetText("UpdateDialog_TwrpNotSelected")
            : Path.GetFileName(SelectedTwrpFilePath);

        /// <summary>
        /// 手动更新文件的友好大小文本。
        /// </summary>
        public string SelectedFileSizeText => GetFileSizeText(SelectedFilePath);

        /// <summary>
        /// 手动 TWRP 文件的友好大小文本。
        /// </summary>
        public string SelectedTwrpFileSizeText => GetFileSizeText(SelectedTwrpFilePath);

        /// <summary>
        /// 当前是否已手动选择更新文件。
        /// </summary>
        public bool HasSelectedFile => !string.IsNullOrWhiteSpace(SelectedFilePath);

        /// <summary>
        /// 当前是否已手动选择 TWRP 镜像。
        /// </summary>
        public bool HasSelectedTwrpFile => !string.IsNullOrWhiteSpace(SelectedTwrpFilePath);

        /// <summary>
        /// 当前模式是否满足开始任务条件。
        /// </summary>
        public bool CanStartCurrentMode => CanStartTask();

        /// <summary>
        /// 选择更新文件命令。
        /// </summary>
        public RelayCommand SelectFileCommand => _selectFileCommand;

        /// <summary>
        /// 选择 TWRP 镜像命令。
        /// </summary>
        public RelayCommand SelectTwrpFileCommand => _selectTwrpFileCommand;

        /// <summary>
        /// 清除手动 TWRP 镜像命令。
        /// </summary>
        public RelayCommand ClearTwrpFileCommand => _clearTwrpFileCommand;

        /// <summary>
        /// 取消并关闭弹窗命令。
        /// </summary>
        public RelayCommand CancelCommand => _cancelCommand;

        /// <summary>
        /// 开始更新任务命令。
        /// </summary>
        public AsyncRelayCommand StartTaskCommand => _startTaskCommand;

        /// <summary>
        /// 选择本地更新文件。
        /// </summary>
        private void SelectFile()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = IsFlashFileMode
                    ? GetText("FileFilter_FlashPackage")
                    : "更新文件 (*.zip;*.ps)|*.zip;*.ps";
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SelectedFilePath = openFileDialog.FileName;
                }
            }
        }

        /// <summary>
        /// 选择本地 TWRP 镜像文件。
        /// </summary>
        private void SelectTwrpFile()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "TWRP 镜像文件 (*.img)|*.img|所有文件 (*.*)|*.*";
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SelectedTwrpFilePath = openFileDialog.FileName;
                }
            }
        }

        /// <summary>
        /// 清除当前手动选择的 TWRP 镜像。
        /// </summary>
        private void ClearTwrpFile()
        {
            SelectedTwrpFilePath = string.Empty;
        }

        /// <summary>
        /// 根据每台设备的代号异步匹配并提取 ROM 与 TWRP。
        /// </summary>
        private async Task RecognizeAutomaticPackagesAsync()
        {
            if (IsRecognizing || AutomaticDevices.Count == 0)
            {
                return;
            }

            IsRecognizing = true;
            try
            {
                foreach (AutomaticUpdateDeviceItemViewModel item in AutomaticDevices)
                {
                    await ResolveAutomaticDeviceAsync(item);
                    foreach (AutomaticUpdateResourceGroupViewModel group in AutomaticResourceGroups)
                    {
                        group.RefreshFileNames(IsFlashRomEnabled, IsFlashTwrpEnabled);
                    }
                }
            }
            finally
            {
                IsRecognizing = false;
                OnPropertyChanged(nameof(CanStartCurrentMode));
                _startTaskCommand.NotifyCanExecuteChanged();
                if (AutomaticDevices.Any(item => !IsAutomaticDeviceResolved(item)))
                {
                    _automaticRecognitionTask = null;
                }
            }
        }

        /// <summary>
        /// 为单台设备提取自动更新文件并更新该行状态。
        /// </summary>
        /// <param name="item">需要识别的设备行。</param>
        private async Task ResolveAutomaticDeviceAsync(AutomaticUpdateDeviceItemViewModel item)
        {
            item.ErrorMessage = string.Empty;
            item.RomFilePath = string.Empty;
            item.TwrpFilePath = string.Empty;
            item.RomVersion = string.Empty;
            item.TwrpVersion = string.Empty;
            item.StatusText = GetText("UpdateDialog_AutoRecognizing");

            try
            {
                if (string.IsNullOrWhiteSpace(item.DeviceCode))
                {
                    throw new InvalidOperationException(GetText("UpdateDialog_AutoCodeMissing"));
                }

                string deviceDirectoryName = SanitizeDirectoryName(item.DeviceCode);
                string extractionRoot = Path.Combine(Path.GetTempPath(), "PolestarSystemTool", "UpdatePackages", deviceDirectoryName);
                RomPackageExtractionResult result = await RomUtil.Instance.ExtractPackagesAsync(
                    item.DeviceCode,
                    Path.Combine(extractionRoot, "ROM"),
                    Path.Combine(extractionRoot, "TWRP"));

                if (!result.HasRom || (!IsFlashFileMode && !result.HasTwrp))
                {
                    string error = string.Join("；", new[] { result.RomError, result.TwrpError }.Where(message => !string.IsNullOrWhiteSpace(message)));
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? GetText("UpdateDialog_AutoMatchFailed") : error);
                }

                item.RomFilePath = result.RomFilePath;
                item.TwrpFilePath = result.TwrpFilePath;
                item.RomVersion = string.IsNullOrWhiteSpace(result.RomPackageVersion) ? result.RomBoard : result.RomPackageVersion;
                item.TwrpVersion = string.IsNullOrWhiteSpace(result.TwrpBuildDate) ? result.TwrpBoard : result.TwrpBuildDate;
                item.StatusText = GetText("UpdateDialog_AutoReady");
            }
            catch (Exception exception)
            {
                item.ErrorMessage = exception.Message;
                item.StatusText = GetText("UpdateDialog_AutoFailed");
                WriteErrorLog("ResolveAutomaticDeviceAsync", item.DeviceCode, exception);
            }
        }

        /// <summary>
        /// 启动当前模式下选中设备的更新任务。
        /// </summary>
        private Task StartTaskAsync()
        {
            if (!CanStartTask())
            {
                return Task.CompletedTask;
            }

            if (IsAutomaticMode)
            {
                foreach (AutomaticUpdateDeviceItemViewModel item in AutomaticDevices)
                {
                    if (IsFlashFileMode)
                    {
                        _startFlashFileTask(item.DeviceItem, item.RomFilePath);
                    }
                    else if (IsFlashRomEnabled)
                    {
                        _startUpdateTask(item.DeviceItem, item.RomFilePath, WipeData, IsFlashTwrpEnabled ? item.TwrpFilePath : string.Empty);
                    }
                    else if (IsFlashTwrpEnabled)
                    {
                        _startTwrpTask(item.DeviceItem, item.TwrpFilePath);
                    }
                }
            }
            else
            {
                foreach (DeviceItemViewModel deviceItem in _selectedDevices.Where(item => item != null))
                {
                    if (IsFlashFileMode)
                    {
                        _startFlashFileTask(deviceItem, SelectedFilePath);
                    }
                    else
                    {
                        _startUpdateTask(deviceItem, SelectedFilePath, WipeData, SelectedTwrpFilePath);
                    }
                }
            }

            Dialog?.Close();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 判断当前模式是否满足开始任务条件。
        /// </summary>
        /// <returns>手动模式已选择 ROM，或自动模式所有设备均匹配成功时返回 true。</returns>
        private bool CanStartTask()
        {
            return IsAutomaticMode
                ? !IsRecognizing &&
                  AutomaticDevices.Count > 0 &&
                  (IsFlashRomEnabled || IsFlashTwrpEnabled) &&
                  AutomaticDevices.All(item =>
                      (!IsFlashRomEnabled || item.HasRom) &&
                      (!IsFlashTwrpEnabled || item.HasTwrp))
                : HasSelectedFile;
        }

        /// <summary>
        /// 应用 ROM、TWRP 勾选状态并按需启动资源识别。
        /// </summary>
        private void UpdateAutomaticResourceSelection()
        {
            OnPropertyChanged(nameof(IsRomLoading));
            OnPropertyChanged(nameof(IsTwrpLoading));
            foreach (AutomaticUpdateResourceGroupViewModel group in AutomaticResourceGroups)
            {
                group.RefreshFileNames(IsFlashRomEnabled, IsFlashTwrpEnabled);
            }

            OnPropertyChanged(nameof(CanStartCurrentMode));
            _startTaskCommand.NotifyCanExecuteChanged();
            StartAutomaticRecognitionIfRequired();
        }

        /// <summary>
        /// 进入自动模式或重新勾选缺失资源时启动一次受控识别任务。
        /// </summary>
        private void StartAutomaticRecognitionIfRequired()
        {
            if (!IsAutomaticMode || IsRecognizing || (!IsFlashRomEnabled && !IsFlashTwrpEnabled))
            {
                return;
            }

            bool needsRecognition = AutomaticDevices.Any(item =>
                (IsFlashRomEnabled && !item.HasRom) ||
                (IsFlashTwrpEnabled && !item.HasTwrp));
            if (needsRecognition && _automaticRecognitionTask == null)
            {
                _automaticRecognitionTask = RecognizeAutomaticPackagesAsync();
            }
        }

        /// <summary>
        /// 判断设备是否已取得当前弹窗模式需要的自动资源。
        /// </summary>
        /// <param name="item">自动识别设备项。</param>
        /// <returns>所需资源均已取得时返回 true。</returns>
        private bool IsAutomaticDeviceResolved(AutomaticUpdateDeviceItemViewModel item)
        {
            return item != null && item.HasRom && (IsFlashFileMode || item.HasTwrp);
        }

        /// <summary>
        /// 关闭当前弹窗。
        /// </summary>
        private void Cancel()
        {
            Dialog?.Close();
        }

        /// <summary>
        /// 获取指定文件的友好大小文本。
        /// </summary>
        /// <param name="filePath">本地文件路径。</param>
        /// <returns>文件不存在时返回空字符串。</returns>
        private static string GetFileSizeText(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return string.Empty;
            }

            long fileLength = new FileInfo(filePath).Length;
            const double oneKilobyte = 1024d;
            const double oneMegabyte = oneKilobyte * 1024d;
            const double oneGigabyte = oneMegabyte * 1024d;
            if (fileLength >= oneGigabyte) return $"({fileLength / oneGigabyte:0.##} GB)";
            if (fileLength >= oneMegabyte) return $"({fileLength / oneMegabyte:0.#} MB)";
            if (fileLength >= oneKilobyte) return $"({fileLength / oneKilobyte:0.#} KB)";
            return $"({fileLength} B)";
        }

        /// <summary>
        /// 将设备代号转换为可用作缓存目录的名称。
        /// </summary>
        /// <param name="deviceCode">设备代号。</param>
        /// <returns>清理后的目录名称。</returns>
        private static string SanitizeDirectoryName(string deviceCode)
        {
            string result = deviceCode.Trim();
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalidCharacter, '_');
            }

            return result;
        }

        /// <summary>
        /// 将选中设备按设备代号分组，确保不同硬件平台分别展示对应资源。
        /// </summary>
        /// <param name="devices">自动识别设备集合。</param>
        /// <returns>按设备代号排序后的资源分组集合。</returns>
        private static ObservableCollection<AutomaticUpdateResourceGroupViewModel> CreateAutomaticResourceGroups(
            IEnumerable<AutomaticUpdateDeviceItemViewModel> devices)
        {
            var groups = (devices ?? Enumerable.Empty<AutomaticUpdateDeviceItemViewModel>())
                .GroupBy(device => GetDeviceGroupName(device.DeviceCode), StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    List<AutomaticUpdateDeviceItemViewModel> groupedDevices = group.ToList();
                    string deviceCountText = string.Format(GetText("UpdateDialog_DeviceCount"), groupedDevices.Count);
                    return new AutomaticUpdateResourceGroupViewModel(group.Key, groupedDevices, deviceCountText);
                });
            return new ObservableCollection<AutomaticUpdateResourceGroupViewModel>(groups);
        }

        /// <summary>
        /// 获取用于自动资源列表分组和展示的设备代号。
        /// </summary>
        /// <param name="deviceCode">设备代号。</param>
        /// <returns>规范化后的设备代号；代号为空时返回占位符。</returns>
        private static string GetDeviceGroupName(string deviceCode)
        {
            string normalizedCode = (deviceCode ?? string.Empty).Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(normalizedCode) ? "-" : normalizedCode;
        }

        /// <summary>
        /// 获取当前语言对应的界面文案。
        /// </summary>
        /// <param name="key">语言资源键。</param>
        /// <returns>本地化文案。</returns>
        private static string GetText(string key)
        {
            return LocalizationService.Instance.GetString(string.Empty, key);
        }

        /// <summary>
        /// 将自动识别异常写入调试日志。
        /// </summary>
        /// <param name="methodName">发生异常的方法。</param>
        /// <param name="deviceCode">设备代号。</param>
        /// <param name="exception">异常对象。</param>
        private static void WriteErrorLog(string methodName, string deviceCode, Exception exception)
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log");
                Directory.CreateDirectory(logDirectory);
                string logPath = Path.Combine(logDirectory, $"automatic_update_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ERROR UpdateSystemDialogViewModel.{methodName} DeviceCode={deviceCode}{Environment.NewLine}{exception}{Environment.NewLine}");
            }
            catch
            {
                // 日志写入失败不能阻断更新弹窗的错误状态展示。
            }
        }
    }
}
