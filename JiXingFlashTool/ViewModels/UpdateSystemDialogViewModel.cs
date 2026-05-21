using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.ItemViewModel;
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
        private readonly Action<DeviceItemViewModel, string, bool> _startUpdateTask;
        private readonly RelayCommand _selectFileCommand;
        private readonly RelayCommand _cancelCommand;
        private readonly RelayCommand _startTaskCommand;
        private string _selectedFilePath = string.Empty;
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
            ? "未选择文件"
            : Path.GetFileName(SelectedFilePath);

        /// <summary>
        /// 当前是否已选择更新文件。
        /// </summary>
        /// <summary>
        /// 褰撳墠鐢ㄤ簬鐣岄潰灞曠ず鐨勬枃浠跺ぇ灏忋€?
        /// </summary>
        public string SelectedFileSizeText => GetSelectedFileSizeText();

        public bool HasSelectedFile => !string.IsNullOrWhiteSpace(SelectedFilePath);

        /// <summary>
        /// 选择文件命令。
        /// </summary>
        public RelayCommand SelectFileCommand => _selectFileCommand;

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
        public UpdateSystemDialogViewModel(IReadOnlyList<DeviceItemViewModel> selectedDevices, Action<DeviceItemViewModel, string, bool> startUpdateTask)
        {
            _selectedDevices = selectedDevices ?? Array.Empty<DeviceItemViewModel>();
            _startUpdateTask = startUpdateTask ?? throw new ArgumentNullException(nameof(startUpdateTask));
            _selectFileCommand = new RelayCommand(SelectFile);
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
                _startUpdateTask(deviceItemViewModel, SelectedFilePath, WipeData);
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
        /// 鑾峰彇宸查€夋枃浠剁殑鍙嬪ソ澶у皬鏂囨湰銆?
        /// </summary>
        /// <returns>鏈€夋枃浠舵垨鏂囦欢涓嶅瓨鍦ㄦ椂杩斿洖绌哄瓧绗︿覆銆?</returns>
        private string GetSelectedFileSizeText()
        {
            if (string.IsNullOrWhiteSpace(SelectedFilePath) || !File.Exists(SelectedFilePath))
            {
                return string.Empty;
            }

            var fileLength = new FileInfo(SelectedFilePath).Length;
            return $"({FormatFileSize(fileLength)})";
        }

        /// <summary>
        /// 灏嗘枃浠跺瓧鑺傛暟杞崲涓虹晫闈㈠睍绀虹殑 MB/KB 鏂囨湰銆?
        /// </summary>
        /// <param name="fileLength">鏂囦欢瀛楄妭鏁般€?/param>
        /// <returns>鏍煎紡鍖栧悗鐨勬枃浠跺ぇ灏忔枃鏈€?</returns>
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
