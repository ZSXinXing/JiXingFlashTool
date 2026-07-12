using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.ItemViewModel;
using LanguageCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace JiXingFlashTool.ViewModels
{
    /// <summary>
    /// 更新文件弹窗视图模型，负责文件选择和更新任务确认。
    /// </summary>
    public class UpdateSystemDialogViewModel : ObservableObject
    {
        private readonly IReadOnlyList<DeviceItemViewModel> _selectedDevices;
        private readonly Action<DeviceItemViewModel, string, bool, string> _startUpdateTask;
        private readonly RelayCommand _selectFileCommand;
        private readonly RelayCommand _selectTwrpFileCommand;
        private readonly RelayCommand _clearTwrpFileCommand;
        private readonly RelayCommand _cancelCommand;
        private readonly RelayCommand _startTaskCommand;
        private string _selectedFilePath = string.Empty;
        private string _selectedTwrpFilePath = string.Empty;
        private bool _wipeData;

        /// <summary>
        /// 当前 HandyControl 弹窗实例。
        /// </summary>
        public Dialog Dialog { get; set; }

        /// <summary>
        /// 已选择的文件绝对路径。
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
                    _startTaskCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 已选择的 TWRP 镜像绝对路径。
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
        /// 当前用于界面展示的文件名。
        /// </summary>
        public string SelectedFileName => string.IsNullOrWhiteSpace(SelectedFilePath)
            ? LocalizationService.Instance.GetString(string.Empty, "UpdateDialog_SystemNotSelected")
            : Path.GetFileName(SelectedFilePath);

        /// <summary>
        /// 当前用于界面展示的 TWRP 镜像文件名。
        /// </summary>
        public string SelectedTwrpFileName => string.IsNullOrWhiteSpace(SelectedTwrpFilePath)
            ? "未选择 TWRP"
            : Path.GetFileName(SelectedTwrpFilePath);

        /// <summary>
        /// 当前用于界面展示的更新文件大小。
        /// </summary>
        public string SelectedFileSizeText => GetSelectedFileSizeText();

        /// <summary>
        /// 当前用于界面展示的 TWRP 镜像文件大小。
        /// </summary>
        public string SelectedTwrpFileSizeText => GetFileSizeText(SelectedTwrpFilePath);

        /// <summary>
        /// 当前是否已选择更新文件。
        /// </summary>
        public bool HasSelectedFile => !string.IsNullOrWhiteSpace(SelectedFilePath);

        /// <summary>
        /// 当前是否已选择 TWRP 镜像。
        /// </summary>
        public bool HasSelectedTwrpFile => !string.IsNullOrWhiteSpace(SelectedTwrpFilePath);

        /// <summary>
        /// 选择文件命令。
        /// </summary>
        public RelayCommand SelectFileCommand => _selectFileCommand;

        /// <summary>
        /// 选择 TWRP 镜像命令。
        /// </summary>
        public RelayCommand SelectTwrpFileCommand => _selectTwrpFileCommand;

        /// <summary>
        /// 清除 TWRP 镜像命令。
        /// </summary>
        public RelayCommand ClearTwrpFileCommand => _clearTwrpFileCommand;

        /// <summary>
        /// 取消命令。
        /// </summary>
        public RelayCommand CancelCommand => _cancelCommand;

        /// <summary>
        /// 开始任务命令。
        /// </summary>
        public RelayCommand StartTaskCommand => _startTaskCommand;

        /// <summary>
        /// 使用选中的设备初始化弹窗。
        /// </summary>
        /// <param name="selectedDevices">当前选中的设备列表。</param>
        /// <param name="startUpdateTask">更新任务入队回调。</param>
        public UpdateSystemDialogViewModel(IReadOnlyList<DeviceItemViewModel> selectedDevices, Action<DeviceItemViewModel, string, bool, string> startUpdateTask)
        {
            _selectedDevices = selectedDevices ?? Array.Empty<DeviceItemViewModel>();
            _startUpdateTask = startUpdateTask ?? throw new ArgumentNullException(nameof(startUpdateTask));
            _selectFileCommand = new RelayCommand(SelectFile);
            _selectTwrpFileCommand = new RelayCommand(SelectTwrpFile);
            _clearTwrpFileCommand = new RelayCommand(ClearTwrpFile);
            _cancelCommand = new RelayCommand(Cancel);
            _startTaskCommand = new RelayCommand(StartTask, CanStartTask);
        }

        /// <summary>
        /// 选择本地更新文件。
        /// </summary>
        private void SelectFile()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "更新文件 (*.zip;*.ps)|*.zip;*.ps";
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
        /// 清除当前选择的 TWRP 镜像文件。
        /// </summary>
        private void ClearTwrpFile()
        {
            SelectedTwrpFilePath = string.Empty;
        }

        /// <summary>
        /// 关闭当前弹窗。
        /// </summary>
        private void Cancel()
        {
            Dialog?.Close();
        }

        /// <summary>
        /// 启动选中设备的更新任务。
        /// </summary>
        private void StartTask()
        {
            if (!CanStartTask())
            {
                return;
            }

            foreach (var deviceItemViewModel in _selectedDevices.Where(item => item != null))
            {
                _startUpdateTask(deviceItemViewModel, SelectedFilePath, WipeData, SelectedTwrpFilePath);
            }

            Dialog?.Close();
        }

        /// <summary>
        /// 判断当前是否满足开始任务条件。
        /// </summary>
        /// <returns>满足条件时返回 true。</returns>
        private bool CanStartTask()
        {
            return HasSelectedFile;
        }

        /// <summary>
        /// 获取已选更新文件的友好大小文本。
        /// </summary>
        /// <returns>未选文件或文件不存在时返回空字符串。</returns>
        private string GetSelectedFileSizeText()
        {
            if (string.IsNullOrWhiteSpace(SelectedFilePath) || !File.Exists(SelectedFilePath))
            {
                return string.Empty;
            }

            return GetFileSizeText(SelectedFilePath);
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

            var fileLength = new FileInfo(filePath).Length;
            return $"({FormatFileSize(fileLength)})";
        }

        /// <summary>
        /// 将文件字节数转换为界面展示的 GB/MB/KB 文本。
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
