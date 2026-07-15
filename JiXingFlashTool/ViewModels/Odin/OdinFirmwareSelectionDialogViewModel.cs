using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.Model;
using JiXingFlashTool.Utils;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace JiXingFlashTool.ViewModels.Odin
{
    /// <summary>
    /// Odin 固件选择弹窗 ViewModel，负责 BL、AP、CSC、USERDATA 文件选择和确认分配。
    /// </summary>
    public sealed class OdinFirmwareSelectionDialogViewModel : ObservableObject
    {
        private readonly Func<OdinFirmwareSelectionDialogViewModel, Task> _confirmAction;
        private string _blFilePath = string.Empty;
        private string _apFilePath = string.Empty;
        private string _twrpFilePath = string.Empty;
        private bool _isManualTwrpMode = true;
        private bool _isLoadingAutomaticTwrpResources;
        private bool _hasLoadedAutomaticTwrpResources;
        private int _automaticTwrpSelectionVersion;
        private TwrpResourceOptionModel _selectedAutomaticTwrpItem;
        private string _systemPackageFilePath = string.Empty;
        private bool _wipeDataBeforeSystemFlash;
        private bool _wipeSystemBeforeSystemFlash;
        private bool _formatDataBeforeSystemFlash;
        private string _cpFilePath = string.Empty;
        private string _cscFilePath = string.Empty;
        private string _userdataFilePath = string.Empty;

        /// <summary>
        /// 初始化 Odin 固件选择弹窗 ViewModel。
        /// </summary>
        /// <param name="selectedDeviceCount">待分配固件的设备数量。</param>
        /// <param name="confirmAction">确认刷入后的异步回调。</param>
        public OdinFirmwareSelectionDialogViewModel(int selectedDeviceCount, Func<OdinFirmwareSelectionDialogViewModel, Task> confirmAction)
        {
            SelectedDeviceCount = selectedDeviceCount;
            _confirmAction = confirmAction;
            SelectBlCommand = new RelayCommand(() => SelectFirmwareFile(value => BlFilePath = value, false));
            SelectApCommand = new RelayCommand(() => SelectFirmwareFile(value => ApFilePath = value, true));
            SelectTwrpCommand = new RelayCommand(() =>
            {
                IsManualTwrpMode = true;
                SelectImageFile(value => TwrpFilePath = value);
            });
            SelectSystemPackageCommand = new RelayCommand(() => SelectSystemPackageFile(value => SystemPackageFilePath = value));
            SelectCpCommand = new RelayCommand(() => SelectFirmwareFile(value => CpFilePath = value, false));
            SelectCscCommand = new RelayCommand(() => SelectFirmwareFile(value => CscFilePath = value, false));
            SelectUserdataCommand = new RelayCommand(() => SelectFirmwareFile(value => UserdataFilePath = value, false));
            ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, CanConfirm);
            CancelCommand = new RelayCommand(Cancel);
            ClearBlCommand = new RelayCommand(() => BlFilePath = string.Empty);
            ClearApCommand = new RelayCommand(() => ApFilePath = string.Empty);
            ClearTwrpCommand = new RelayCommand(ClearTwrpSelection);
            ClearSystemPackageCommand = new RelayCommand(() => SystemPackageFilePath = string.Empty);
            ClearCpCommand = new RelayCommand(() => CpFilePath = string.Empty);
            ClearCscCommand = new RelayCommand(() => CscFilePath = string.Empty);
            ClearUserdataCommand = new RelayCommand(() => UserdataFilePath = string.Empty);
        }

        /// <summary>
        /// 当前 HandyControl 弹窗实例，用于命令关闭弹窗。
        /// </summary>
        public Dialog Dialog { get; set; }

        /// <summary>
        /// 待分配固件的设备数量。
        /// </summary>
        public int SelectedDeviceCount { get; }

        /// <summary>
        /// 弹窗标题旁显示的设备数量标签文本。
        /// </summary>
        public string SelectedDeviceTagText => $"将分配给 {SelectedDeviceCount} 台设备";

        /// <summary>
        /// BL 固件文件路径，未选择时为空。
        /// </summary>
        public string BlFilePath
        {
            get => _blFilePath;
            set => SetFirmwarePath(ref _blFilePath, value, nameof(BlFilePath), nameof(BlFileName), nameof(BlFileSizeText), nameof(HasBlFile));
        }

        /// <summary>
        /// AP 固件文件路径，Odin 普通刷机和 TWRP 刷入都必须选择。
        /// </summary>
        public string ApFilePath
        {
            get => _apFilePath;
            set => SetFirmwarePath(ref _apFilePath, value, nameof(ApFilePath), nameof(ApFileName), nameof(ApFileSizeText), nameof(HasApFile));
        }

        /// <summary>
        /// 用于替换 AP 内置 Recovery 的 TWRP 镜像路径，未选择时为空。
        /// </summary>
        public string TwrpFilePath
        {
            get => _twrpFilePath;
            set => SetFirmwarePath(ref _twrpFilePath, value, nameof(TwrpFilePath), nameof(TwrpFileName), nameof(TwrpFileSizeText), nameof(HasTwrpFile));
        }

        /// <summary>
        /// TWRP 手动选择模式是否启用，启用时通过本地文件选择器选择 .img。
        /// </summary>
        public bool IsManualTwrpMode
        {
            get => _isManualTwrpMode;
            set
            {
                if (SetProperty(ref _isManualTwrpMode, value))
                {
                    OnPropertyChanged(nameof(IsAutomaticTwrpMode));
                    if (value)
                    {
                        _automaticTwrpSelectionVersion++;
                        SelectedAutomaticTwrpItem = null;
                    }
                }
            }
        }

        /// <summary>
        /// TWRP 自动选择模式是否启用，启用时从资源包下拉选择版本。
        /// </summary>
        public bool IsAutomaticTwrpMode
        {
            get => !IsManualTwrpMode;
            set
            {
                if (value)
                {
                    IsManualTwrpMode = false;
                    _ = LoadAutomaticTwrpResourcesAsync();
                }
                else
                {
                    IsManualTwrpMode = true;
                }
            }
        }

        /// <summary>
        /// 自动模式下可选择的 TWRP 资源条目集合。
        /// </summary>
        public ObservableCollection<TwrpResourceOptionModel> AutomaticTwrpItems { get; } = new ObservableCollection<TwrpResourceOptionModel>();

        /// <summary>
        /// 自动模式下当前选中的 TWRP 资源条目。
        /// </summary>
        public TwrpResourceOptionModel SelectedAutomaticTwrpItem
        {
            get => _selectedAutomaticTwrpItem;
            set
            {
                if (SetProperty(ref _selectedAutomaticTwrpItem, value))
                {
                    int selectionVersion = ++_automaticTwrpSelectionVersion;
                    _ = ApplyAutomaticTwrpItemAsync(value, selectionVersion);
                }
            }
        }

        /// <summary>
        /// 是否正在读取或提取自动 TWRP 资源。
        /// </summary>
        public bool IsLoadingAutomaticTwrpResources
        {
            get => _isLoadingAutomaticTwrpResources;
            set => SetProperty(ref _isLoadingAutomaticTwrpResources, value);
        }

        /// <summary>
        /// TWRP 刷入后需要侧载安装的系统包路径，未选择时为空。
        /// </summary>
        public string SystemPackageFilePath
        {
            get => _systemPackageFilePath;
            set
            {
                if (SetFirmwarePath(ref _systemPackageFilePath, value, nameof(SystemPackageFilePath), nameof(SystemPackageFileName), nameof(SystemPackageFileSizeText), nameof(HasSystemPackageFile)))
                {
                    WipeDataBeforeSystemFlash = HasSystemPackageFile;
                    WipeSystemBeforeSystemFlash = HasSystemPackageFile;
                    FormatDataBeforeSystemFlash = HasSystemPackageFile;
                }
            }
        }

        /// <summary>
        /// 刷入系统包前是否执行双清。
        /// </summary>
        public bool WipeDataBeforeSystemFlash
        {
            get => _wipeDataBeforeSystemFlash;
            set => SetProperty(ref _wipeDataBeforeSystemFlash, value);
        }

        /// <summary>
        /// 刷入系统包前是否清除 system 分区。
        /// </summary>
        public bool WipeSystemBeforeSystemFlash
        {
            get => _wipeSystemBeforeSystemFlash;
            set => SetProperty(ref _wipeSystemBeforeSystemFlash, value);
        }

        /// <summary>
        /// 刷入系统包前是否格式化 data 分区。
        /// </summary>
        public bool FormatDataBeforeSystemFlash
        {
            get => _formatDataBeforeSystemFlash;
            set => SetProperty(ref _formatDataBeforeSystemFlash, value);
        }

        /// <summary>
        /// CP 固件文件路径，未选择时为空。
        /// </summary>
        public string CpFilePath
        {
            get => _cpFilePath;
            set => SetFirmwarePath(ref _cpFilePath, value, nameof(CpFilePath), nameof(CpFileName), nameof(CpFileSizeText), nameof(HasCpFile));
        }

        /// <summary>
        /// CSC 固件文件路径，未选择时为空。
        /// </summary>
        public string CscFilePath
        {
            get => _cscFilePath;
            set => SetFirmwarePath(ref _cscFilePath, value, nameof(CscFilePath), nameof(CscFileName), nameof(CscFileSizeText), nameof(HasCscFile));
        }

        /// <summary>
        /// USERDATA 固件文件路径，未选择时为空。
        /// </summary>
        public string UserdataFilePath
        {
            get => _userdataFilePath;
            set => SetFirmwarePath(ref _userdataFilePath, value, nameof(UserdataFilePath), nameof(UserdataFileName), nameof(UserdataFileSizeText), nameof(HasUserdataFile));
        }

        /// <summary>
        /// BL 固件文件名。
        /// </summary>
        public string BlFileName => Path.GetFileName(BlFilePath);

        /// <summary>
        /// AP 固件文件名。
        /// </summary>
        public string ApFileName => Path.GetFileName(ApFilePath);

        /// <summary>
        /// TWRP 镜像文件名。
        /// </summary>
        public string TwrpFileName => Path.GetFileName(TwrpFilePath);

        /// <summary>
        /// 系统包文件名。
        /// </summary>
        public string SystemPackageFileName => Path.GetFileName(SystemPackageFilePath);

        /// <summary>
        /// CP 固件文件名。
        /// </summary>
        public string CpFileName => Path.GetFileName(CpFilePath);

        /// <summary>
        /// CSC 固件文件名。
        /// </summary>
        public string CscFileName => Path.GetFileName(CscFilePath);

        /// <summary>
        /// USERDATA 固件文件名。
        /// </summary>
        public string UserdataFileName => Path.GetFileName(UserdataFilePath);

        /// <summary>
        /// BL 固件文件大小显示文本。
        /// </summary>
        public string BlFileSizeText => GetFileSizeText(BlFilePath);

        /// <summary>
        /// AP 固件文件大小显示文本。
        /// </summary>
        public string ApFileSizeText => GetFileSizeText(ApFilePath);

        /// <summary>
        /// TWRP 镜像文件大小显示文本。
        /// </summary>
        public string TwrpFileSizeText => GetFileSizeText(TwrpFilePath);

        /// <summary>
        /// 系统包文件大小显示文本。
        /// </summary>
        public string SystemPackageFileSizeText => GetFileSizeText(SystemPackageFilePath);

        /// <summary>
        /// CP 固件文件大小显示文本。
        /// </summary>
        public string CpFileSizeText => GetFileSizeText(CpFilePath);

        /// <summary>
        /// CSC 固件文件大小显示文本。
        /// </summary>
        public string CscFileSizeText => GetFileSizeText(CscFilePath);

        /// <summary>
        /// USERDATA 固件文件大小显示文本。
        /// </summary>
        public string UserdataFileSizeText => GetFileSizeText(UserdataFilePath);

        /// <summary>
        /// 是否已经选择 BL 固件。
        /// </summary>
        public bool HasBlFile => !string.IsNullOrWhiteSpace(BlFilePath);

        /// <summary>
        /// 是否已经选择 AP 固件。
        /// </summary>
        public bool HasApFile => !string.IsNullOrWhiteSpace(ApFilePath);

        /// <summary>
        /// 是否已经选择 TWRP 镜像。
        /// </summary>
        public bool HasTwrpFile => !string.IsNullOrWhiteSpace(TwrpFilePath);

        /// <summary>
        /// 系统包选择区域是否常驻显示。
        /// </summary>
        public bool IsSystemPackageSectionVisible => true;

        /// <summary>
        /// 是否已经选择 TWRP 侧载系统包。
        /// </summary>
        public bool HasSystemPackageFile => !string.IsNullOrWhiteSpace(SystemPackageFilePath);

        /// <summary>
        /// 是否已经选择 CP 固件。
        /// </summary>
        public bool HasCpFile => !string.IsNullOrWhiteSpace(CpFilePath);

        /// <summary>
        /// 是否已经选择 CSC 固件。
        /// </summary>
        public bool HasCscFile => !string.IsNullOrWhiteSpace(CscFilePath);

        /// <summary>
        /// 是否已经选择 USERDATA 固件。
        /// </summary>
        public bool HasUserdataFile => !string.IsNullOrWhiteSpace(UserdataFilePath);

        /// <summary>
        /// 选择 BL 固件文件命令。
        /// </summary>
        public RelayCommand SelectBlCommand { get; }

        /// <summary>
        /// 选择 AP 固件文件命令。
        /// </summary>
        public RelayCommand SelectApCommand { get; }

        /// <summary>
        /// 选择 TWRP 镜像文件命令。
        /// </summary>
        public RelayCommand SelectTwrpCommand { get; }

        /// <summary>
        /// 选择 TWRP 侧载系统包命令。
        /// </summary>
        public RelayCommand SelectSystemPackageCommand { get; }

        /// <summary>
        /// 选择 CP 固件文件命令。
        /// </summary>
        public RelayCommand SelectCpCommand { get; }

        /// <summary>
        /// 选择 CSC 固件文件命令。
        /// </summary>
        public RelayCommand SelectCscCommand { get; }

        /// <summary>
        /// 选择 USERDATA 固件文件命令。
        /// </summary>
        public RelayCommand SelectUserdataCommand { get; }

        /// <summary>
        /// 清除 BL 固件文件命令。
        /// </summary>
        public RelayCommand ClearBlCommand { get; }

        /// <summary>
        /// 清除 AP 固件文件命令。
        /// </summary>
        public RelayCommand ClearApCommand { get; }

        /// <summary>
        /// 清除 TWRP 镜像文件命令。
        /// </summary>
        public RelayCommand ClearTwrpCommand { get; }

        /// <summary>
        /// 清除 TWRP 侧载系统包命令。
        /// </summary>
        public RelayCommand ClearSystemPackageCommand { get; }

        /// <summary>
        /// 清除 CP 固件文件命令。
        /// </summary>
        public RelayCommand ClearCpCommand { get; }

        /// <summary>
        /// 清除 CSC 固件文件命令。
        /// </summary>
        public RelayCommand ClearCscCommand { get; }

        /// <summary>
        /// 清除 USERDATA 固件文件命令。
        /// </summary>
        public RelayCommand ClearUserdataCommand { get; }

        /// <summary>
        /// 确认刷入固件命令。
        /// </summary>
        public AsyncRelayCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消并关闭弹窗命令。
        /// </summary>
        public RelayCommand CancelCommand { get; }

        /// <summary>
        /// 判断确认按钮是否可用，业务上仅 AP 为必选固件。
        /// </summary>
        /// <returns>已经选择 AP 时返回 true。</returns>
        private bool CanConfirm()
        {
            return HasApFile;
        }

        /// <summary>
        /// 确认刷入当前选择的固件并关闭弹窗。
        /// </summary>
        /// <returns>异步刷入任务。</returns>
        private async Task ConfirmAsync()
        {
            if (_confirmAction != null)
            {
                await _confirmAction(this);
            }

            Dialog?.Close();
        }

        /// <summary>
        /// 取消选择并关闭弹窗。
        /// </summary>
        private void Cancel()
        {
            Dialog?.Close();
        }

        /// <summary>
        /// 清除当前 TWRP 选择，手动文件与自动资源选中项都会同步清空。
        /// </summary>
        private void ClearTwrpSelection()
        {
            SelectedAutomaticTwrpItem = null;
            TwrpFilePath = string.Empty;
        }

        /// <summary>
        /// 异步读取已配置资源包中的 TWRP 镜像条目，避免切换自动模式时阻塞界面。
        /// </summary>
        /// <returns>异步加载任务。</returns>
        private async Task LoadAutomaticTwrpResourcesAsync()
        {
            if (_hasLoadedAutomaticTwrpResources || IsLoadingAutomaticTwrpResources)
            {
                return;
            }

            IsLoadingAutomaticTwrpResources = true;
            try
            {
                var items = await Task.Run(() => RomUtil.Instance.GetTwrpResourceOptions());
                AutomaticTwrpItems.Clear();
                foreach (TwrpResourceOptionModel item in items)
                {
                    AutomaticTwrpItems.Add(item);
                }

                _hasLoadedAutomaticTwrpResources = true;
            }
            catch (Exception exception)
            {
                WriteTwrpResourceLog(nameof(LoadAutomaticTwrpResourcesAsync), exception);
                Growl.Warning("未读取到 TWRP 资源，请先在资源中配置 TWRP。");
            }
            finally
            {
                IsLoadingAutomaticTwrpResources = false;
            }
        }

        /// <summary>
        /// 将自动模式选中的 TWRP 资源提取到运行目录，并写入 Odin 刷机使用的 TWRP 路径。
        /// </summary>
        /// <param name="item">当前下拉选中的 TWRP 资源条目。</param>
        /// <returns>异步提取任务。</returns>
        private async Task ApplyAutomaticTwrpItemAsync(TwrpResourceOptionModel item, int selectionVersion)
        {
            if (item == null)
            {
                if (selectionVersion == _automaticTwrpSelectionVersion)
                {
                    TwrpFilePath = string.Empty;
                }

                return;
            }

            IsLoadingAutomaticTwrpResources = true;
            try
            {
                string targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JXHeimdallExtract", "OdinTwrpResource", CreateTwrpResourceCacheDirectoryName(item));
                string extractedTwrpPath = await Task.Run(() => RomUtil.Instance.ExtractTwrpResource(item, targetDirectory));
                if (selectionVersion == _automaticTwrpSelectionVersion && ReferenceEquals(item, SelectedAutomaticTwrpItem))
                {
                    TwrpFilePath = extractedTwrpPath;
                }
            }
            catch (Exception exception)
            {
                WriteTwrpResourceLog(nameof(ApplyAutomaticTwrpItemAsync), exception);
                if (selectionVersion == _automaticTwrpSelectionVersion)
                {
                    TwrpFilePath = string.Empty;
                    Growl.Error("TWRP 资源提取失败。");
                }
            }
            finally
            {
                if (selectionVersion == _automaticTwrpSelectionVersion)
                {
                    IsLoadingAutomaticTwrpResources = false;
                }
            }
        }

        /// <summary>
        /// 为自动 TWRP 资源生成独立缓存目录名，避免不同版本同名镜像互相复用。
        /// </summary>
        /// <param name="item">TWRP 资源条目。</param>
        /// <returns>可作为本地目录名使用的缓存键。</returns>
        private static string CreateTwrpResourceCacheDirectoryName(TwrpResourceOptionModel item)
        {
            string rawName = $"{item.Series}_{item.Board}_{item.BuildDate}_{Path.GetFileNameWithoutExtension(item.ImageFile)}";
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                rawName = rawName.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(rawName) ? "default" : rawName;
        }

        /// <summary>
        /// 记录 Odin TWRP 自动资源读取与提取异常，便于后续根据日志定位问题。
        /// </summary>
        /// <param name="methodName">发生异常的方法名。</param>
        /// <param name="exception">异常对象。</param>
        private static void WriteTwrpResourceLog(string methodName, Exception exception)
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log");
                Directory.CreateDirectory(logDirectory);
                string logPath = Path.Combine(logDirectory, $"odin_twrp_resource_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ERROR OdinFirmwareSelectionDialogViewModel.{methodName}{Environment.NewLine}{exception}{Environment.NewLine}");
            }
            catch
            {
            }
        }

        /// <summary>
        /// 打开文件选择器并将选中的 Odin 固件路径写入目标槽位。
        /// </summary>
        /// <param name="assignAction">接收文件路径的槽位赋值回调。</param>
        private void SelectFirmwareFile(Action<string> assignAction, bool allowImageFile)
        {
            var dialog = new OpenFileDialog
            {
                Filter = allowImageFile
                    ? "Odin AP/TWRP 文件 (*.tar;*.md5;*.tar.md5;*.img)|*.tar;*.md5;*.tar.md5;*.img|所有文件 (*.*)|*.*"
                    : "Odin 固件文件 (*.tar;*.md5;*.tar.md5)|*.tar;*.md5;*.tar.md5|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                assignAction(dialog.FileName);
            }
        }

        /// <summary>
        /// 打开 TWRP 镜像文件选择器。
        /// </summary>
        /// <param name="assignAction">接收文件路径的赋值回调。</param>
        private void SelectImageFile(Action<string> assignAction)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "TWRP 镜像文件 (*.img)|*.img|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                assignAction(dialog.FileName);
            }
        }

        /// <summary>
        /// 打开 TWRP 侧载系统包选择器。
        /// </summary>
        /// <param name="assignAction">接收文件路径的赋值回调。</param>
        private void SelectSystemPackageFile(Action<string> assignAction)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "TWRP 系统包 (*.zip;*.ps)|*.zip;*.ps|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                assignAction(dialog.FileName);
            }
        }

        /// <summary>
        /// 更新固件路径，并同步刷新文件名、文件大小和命令状态。
        /// </summary>
        /// <param name="field">待更新的路径字段。</param>
        /// <param name="value">新的文件路径。</param>
        /// <param name="propertyName">路径属性名称。</param>
        /// <param name="fileNamePropertyName">文件名属性名称。</param>
        /// <param name="fileSizePropertyName">文件大小属性名称。</param>
        /// <param name="hasFilePropertyName">是否已选择属性名称。</param>
        private bool SetFirmwarePath(ref string field, string value, string propertyName, string fileNamePropertyName, string fileSizePropertyName, string hasFilePropertyName)
        {
            if (SetProperty(ref field, value, propertyName))
            {
                OnPropertyChanged(fileNamePropertyName);
                OnPropertyChanged(fileSizePropertyName);
                OnPropertyChanged(hasFilePropertyName);
                ConfirmCommand.NotifyCanExecuteChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取文件大小显示文本，文件不存在时返回空文本。
        /// </summary>
        /// <param name="filePath">固件文件路径。</param>
        /// <returns>带括号的文件大小文本。</returns>
        private static string GetFileSizeText(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return string.Empty;
            }

            var fileLength = new FileInfo(filePath).Length;
            return $"({FormatFileSize(fileLength)})";
        }

        /// <summary>
        /// 将文件字节数格式化为适合界面展示的单位。
        /// </summary>
        /// <param name="fileLength">文件字节数。</param>
        /// <returns>格式化后的文件大小文本。</returns>
        private static string FormatFileSize(long fileLength)
        {
            const double oneKilobyte = 1024d;
            const double oneMegabyte = oneKilobyte * 1024d;
            const double oneGigabyte = oneMegabyte * 1024d;

            if (fileLength >= oneGigabyte)
            {
                return $"{fileLength / oneGigabyte:0.##} GB";
            }

            if (fileLength >= oneMegabyte)
            {
                return $"{fileLength / oneMegabyte:0.#} MB";
            }

            if (fileLength >= oneKilobyte)
            {
                return $"{fileLength / oneKilobyte:0.#} KB";
            }

            return $"{fileLength} B";
        }
    }
}
