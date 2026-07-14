using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using Microsoft.Win32;
using System;
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
            SelectTwrpCommand = new RelayCommand(() => SelectImageFile(value => TwrpFilePath = value));
            SelectSystemPackageCommand = new RelayCommand(() => SelectSystemPackageFile(value => SystemPackageFilePath = value));
            SelectCpCommand = new RelayCommand(() => SelectFirmwareFile(value => CpFilePath = value, false));
            SelectCscCommand = new RelayCommand(() => SelectFirmwareFile(value => CscFilePath = value, false));
            SelectUserdataCommand = new RelayCommand(() => SelectFirmwareFile(value => UserdataFilePath = value, false));
            ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, CanConfirm);
            CancelCommand = new RelayCommand(Cancel);
            ClearBlCommand = new RelayCommand(() => BlFilePath = string.Empty);
            ClearApCommand = new RelayCommand(() => ApFilePath = string.Empty);
            ClearTwrpCommand = new RelayCommand(() => TwrpFilePath = string.Empty);
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
