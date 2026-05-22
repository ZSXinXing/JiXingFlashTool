using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using Microsoft.Win32;
using System;
using System.IO;

namespace JiXingFlashTool.ViewModels.Odin
{
    /// <summary>
    /// 固件选择弹窗 ViewModel，负责 BL、AP、CSC、USERDATA 文件选择和确认分配。
    /// </summary>
    public sealed class OdinFirmwareSelectionDialogViewModel : ObservableObject
    {
        private readonly Action<OdinFirmwareSelectionDialogViewModel> _confirmAction;
        private string _blFilePath = string.Empty;
        private string _apFilePath = string.Empty;
        private string _cscFilePath = string.Empty;
        private string _userdataFilePath = string.Empty;

        /// <summary>
        /// 初始化固件选择弹窗 ViewModel。
        /// </summary>
        /// <param name="selectedDeviceCount">待分配设备数量。</param>
        /// <param name="confirmAction">确认后的回调。</param>
        public OdinFirmwareSelectionDialogViewModel(int selectedDeviceCount, Action<OdinFirmwareSelectionDialogViewModel> confirmAction)
        {
            SelectedDeviceCount = selectedDeviceCount;
            _confirmAction = confirmAction;
            SelectBlCommand = new RelayCommand(() => SelectFirmwareFile(value => BlFilePath = value));
            SelectApCommand = new RelayCommand(() => SelectFirmwareFile(value => ApFilePath = value));
            SelectCscCommand = new RelayCommand(() => SelectFirmwareFile(value => CscFilePath = value));
            SelectUserdataCommand = new RelayCommand(() => SelectFirmwareFile(value => UserdataFilePath = value));
            ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
            CancelCommand = new RelayCommand(Cancel);
            ClearBlCommand = new RelayCommand(() => BlFilePath = string.Empty);
            ClearApCommand = new RelayCommand(() => ApFilePath = string.Empty);
            ClearCscCommand = new RelayCommand(() => CscFilePath = string.Empty);
            ClearUserdataCommand = new RelayCommand(() => UserdataFilePath = string.Empty);
        }

        /// <summary>
        /// 当前 HandyControl 弹窗实例。
        /// </summary>
        public Dialog Dialog { get; set; }

        /// <summary>
        /// 待分配设备数量。
        /// </summary>
        public int SelectedDeviceCount { get; }

        /// <summary>
        /// 弹窗标题标签文本。
        /// </summary>
        public string SelectedDeviceTagText => $"将分配给 {SelectedDeviceCount} 台设备";

        /// <summary>
        /// BL 固件路径。
        /// </summary>
        public string BlFilePath
        {
            get => _blFilePath;
            set => SetFirmwarePath(ref _blFilePath, value, nameof(BlFilePath), nameof(BlFileName), nameof(HasBlFile));
        }

        /// <summary>
        /// AP 固件路径。
        /// </summary>
        public string ApFilePath
        {
            get => _apFilePath;
            set => SetFirmwarePath(ref _apFilePath, value, nameof(ApFilePath), nameof(ApFileName), nameof(HasApFile));
        }

        /// <summary>
        /// CSC 固件路径。
        /// </summary>
        public string CscFilePath
        {
            get => _cscFilePath;
            set => SetFirmwarePath(ref _cscFilePath, value, nameof(CscFilePath), nameof(CscFileName), nameof(HasCscFile));
        }

        /// <summary>
        /// USERDATA 固件路径。
        /// </summary>
        public string UserdataFilePath
        {
            get => _userdataFilePath;
            set => SetFirmwarePath(ref _userdataFilePath, value, nameof(UserdataFilePath), nameof(UserdataFileName), nameof(HasUserdataFile));
        }

        /// <summary>
        /// BL 文件名。
        /// </summary>
        public string BlFileName => Path.GetFileName(BlFilePath);

        /// <summary>
        /// AP 文件名。
        /// </summary>
        public string ApFileName => Path.GetFileName(ApFilePath);

        /// <summary>
        /// CSC 文件名。
        /// </summary>
        public string CscFileName => Path.GetFileName(CscFilePath);

        /// <summary>
        /// USERDATA 文件名。
        /// </summary>
        public string UserdataFileName => Path.GetFileName(UserdataFilePath);

        /// <summary>
        /// 是否已选择 BL。
        /// </summary>
        public bool HasBlFile => !string.IsNullOrWhiteSpace(BlFilePath);

        /// <summary>
        /// 是否已选择 AP。
        /// </summary>
        public bool HasApFile => !string.IsNullOrWhiteSpace(ApFilePath);

        /// <summary>
        /// 是否已选择 CSC。
        /// </summary>
        public bool HasCscFile => !string.IsNullOrWhiteSpace(CscFilePath);

        /// <summary>
        /// 是否已选择 USERDATA。
        /// </summary>
        public bool HasUserdataFile => !string.IsNullOrWhiteSpace(UserdataFilePath);

        /// <summary>
        /// 选择 BL 文件命令。
        /// </summary>
        public RelayCommand SelectBlCommand { get; }

        /// <summary>
        /// 选择 AP 文件命令。
        /// </summary>
        public RelayCommand SelectApCommand { get; }

        /// <summary>
        /// 选择 CSC 文件命令。
        /// </summary>
        public RelayCommand SelectCscCommand { get; }

        /// <summary>
        /// 选择 USERDATA 文件命令。
        /// </summary>
        public RelayCommand SelectUserdataCommand { get; }

        /// <summary>
        /// 清除 BL 文件命令。
        /// </summary>
        public RelayCommand ClearBlCommand { get; }

        /// <summary>
        /// 清除 AP 文件命令。
        /// </summary>
        public RelayCommand ClearApCommand { get; }

        /// <summary>
        /// 清除 CSC 文件命令。
        /// </summary>
        public RelayCommand ClearCscCommand { get; }

        /// <summary>
        /// 清除 USERDATA 文件命令。
        /// </summary>
        public RelayCommand ClearUserdataCommand { get; }

        /// <summary>
        /// 确认分配固件命令。
        /// </summary>
        public RelayCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消弹窗命令。
        /// </summary>
        public RelayCommand CancelCommand { get; }

        private bool CanConfirm()
        {
            return HasBlFile && HasApFile;
        }

        private void Confirm()
        {
            _confirmAction?.Invoke(this);
            Dialog?.Close();
        }

        private void Cancel()
        {
            Dialog?.Close();
        }

        private void SelectFirmwareFile(Action<string> assignAction)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Odin 固件文件 (*.tar;*.md5;*.tar.md5)|*.tar;*.md5;*.tar.md5|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                assignAction(dialog.FileName);
            }
        }

        private void SetFirmwarePath(ref string field, string value, string propertyName, string fileNamePropertyName, string hasFilePropertyName)
        {
            if (SetProperty(ref field, value, propertyName))
            {
                OnPropertyChanged(fileNamePropertyName);
                OnPropertyChanged(hasFilePropertyName);
                ConfirmCommand.NotifyCanExecuteChanged();
            }
        }
    }
}
