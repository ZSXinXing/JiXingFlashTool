using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LanguageCore;
using Microsoft.Win32;
using System;
using System.IO;

namespace JiXingFlashTool.ItemViewModel
{
    /// <summary>
    /// 资源文件单行视图模型，负责单个型号资源文件的展示、选择和清空。
    /// </summary>
    public partial class ResourceFileItemViewModel : ObservableObject
    {
        private readonly string _fileFilter;
        private readonly string _placeholderResourceKey;

        /// <summary>
        /// 型号系列名称。
        /// </summary>
        public string SeriesName { get; }

        /// <summary>
        /// 未选择文件时显示的占位语言键。
        /// </summary>
        public string PlaceholderResourceKey => _placeholderResourceKey;

        /// <summary>
        /// 当前选中的本地文件路径。
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayFileName))]
        [NotifyPropertyChangedFor(nameof(HasSelectedFile))]
        private string _filePath = string.Empty;

        /// <summary>
        /// 当前展示的文件名，未选择时返回占位文本。
        /// </summary>
        public string DisplayFileName => string.IsNullOrWhiteSpace(FilePath) ? GetLangText(PlaceholderResourceKey) : Path.GetFileName(FilePath);

        /// <summary>
        /// 是否已经选择本地文件。
        /// </summary>
        public bool HasSelectedFile => !string.IsNullOrWhiteSpace(FilePath);

        /// <summary>
        /// 初始化资源文件单行视图模型。
        /// </summary>
        /// <param name="seriesName">型号系列名称。</param>
        /// <param name="placeholderResourceKey">未选择文件时显示的占位语言键。</param>
        /// <param name="fileFilter">文件选择器过滤条件。</param>
        public ResourceFileItemViewModel(string seriesName, string placeholderResourceKey, string fileFilter)
        {
            SeriesName = seriesName ?? throw new ArgumentNullException(nameof(seriesName));
            _placeholderResourceKey = placeholderResourceKey ?? string.Empty;
            _fileFilter = string.IsNullOrWhiteSpace(fileFilter) ? "All files (*.*)|*.*" : fileFilter;
        }

        /// <summary>
        /// 语言切换后刷新当前行的占位展示文本。
        /// </summary>
        public void RefreshLanguage()
        {
            OnPropertyChanged(nameof(DisplayFileName));
        }

        /// <summary>
        /// 打开文件选择器并记录当前型号选择的文件。
        /// </summary>
        [RelayCommand]
        private void SelectFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = _fileFilter,
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                FilePath = dialog.FileName;
            }
        }

        /// <summary>
        /// 清空当前型号已选择的文件。
        /// </summary>
        [RelayCommand]
        private void ClearFile()
        {
            FilePath = string.Empty;
        }

        /// <summary>
        /// 获取当前语言资源文本。
        /// </summary>
        /// <param name="key">语言资源键。</param>
        /// <returns>当前语言文本。</returns>
        private static string GetLangText(string key)
        {
            return LocalizationService.Instance.GetString(string.Empty, key);
        }
    }
}
