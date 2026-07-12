using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Model;
using JiXingFlashTool.Services;
using LanguageCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace JiXingFlashTool.ViewModels
{
    /// <summary>
    /// 资源管理弹窗视图模型，负责系统固件与 TWRP 资源 Tab 的状态和本地文件选择。
    /// </summary>
    public partial class ResourceManagementViewModel : ObservableObject
    {
        private readonly ObservableCollection<ResourceFileItemViewModel> _systemFirmwareItems;
        private readonly ObservableCollection<ResourceFileItemViewModel> _twrpItems;

        /// <summary>
        /// 当前弹窗展示的资源文件行集合。
        /// </summary>
        public ObservableCollection<ResourceFileItemViewModel> ResourceItems { get; } = new ObservableCollection<ResourceFileItemViewModel>();

        /// <summary>
        /// 当前 HandyControl 资源管理弹窗实例。
        /// </summary>
        public Dialog Dialog { get; set; }

        /// <summary>
        /// 当前是否选中系统固件 Tab。
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SystemFirmwareTabForeground))]
        [NotifyPropertyChangedFor(nameof(SystemFirmwareTabBorderBrush))]
        [NotifyPropertyChangedFor(nameof(SystemFirmwareTabBackground))]
        [NotifyPropertyChangedFor(nameof(TwrpTabForeground))]
        [NotifyPropertyChangedFor(nameof(TwrpTabBorderBrush))]
        [NotifyPropertyChangedFor(nameof(TwrpTabBackground))]
        [NotifyPropertyChangedFor(nameof(SeriesTagBackground))]
        [NotifyPropertyChangedFor(nameof(SeriesTagForeground))]
        private bool _isSystemFirmwareTab = true;

        /// <summary>
        /// 当前是否正在保存资源路径。
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private bool _isSaving;

        /// <summary>
        /// 系统固件 Tab 前景色。
        /// </summary>
        public string SystemFirmwareTabForeground => IsSystemFirmwareTab ? "#2196F3" : "#999999";

        /// <summary>
        /// 系统固件 Tab 底部边框色。
        /// </summary>
        public string SystemFirmwareTabBorderBrush => IsSystemFirmwareTab ? "#2196F3" : "Transparent";

        /// <summary>
        /// 系统固件 Tab 背景色。
        /// </summary>
        public string SystemFirmwareTabBackground => IsSystemFirmwareTab ? "#4DF5F5F5" : "Transparent";

        /// <summary>
        /// TWRP Tab 前景色。
        /// </summary>
        public string TwrpTabForeground => IsSystemFirmwareTab ? "#999999" : "#FF9800";

        /// <summary>
        /// TWRP Tab 底部边框色。
        /// </summary>
        public string TwrpTabBorderBrush => IsSystemFirmwareTab ? "Transparent" : "#FF9800";

        /// <summary>
        /// TWRP Tab 背景色。
        /// </summary>
        public string TwrpTabBackground => IsSystemFirmwareTab ? "Transparent" : "#4DF5F5F5";

        /// <summary>
        /// 型号标签背景色。
        /// </summary>
        public string SeriesTagBackground => IsSystemFirmwareTab ? "#1A2196F3" : "#1AFF9800";

        /// <summary>
        /// 型号标签前景色。
        /// </summary>
        public string SeriesTagForeground => IsSystemFirmwareTab ? "#2196F3" : "#FF9800";

        /// <summary>
        /// 初始化资源管理弹窗视图模型。
        /// </summary>
        public ResourceManagementViewModel()
        {
            _systemFirmwareItems = CreateResourceItems("FileFilter_7z", "Resource_SelectS7File", "Resource_SelectS8File", "Resource_SelectS9File");
            _twrpItems = CreateResourceItems("FileFilter_7z", "Resource_SelectS7TwrpFile", "Resource_SelectS8TwrpFile", "Resource_SelectS9TwrpFile");
            WeakEventManager<LocalizationService, EventArgs>.AddHandler(
                LocalizationService.Instance,
                nameof(LocalizationService.LanguageChanged),
                OnLanguageChanged);
            LoadSavedFilePaths();
            RefreshResourceItems();
        }

        /// <summary>
        /// 切换到系统固件 Tab。
        /// </summary>
        [RelayCommand]
        private void ShowSystemFirmware()
        {
            IsSystemFirmwareTab = true;
            RefreshResourceItems();
        }

        /// <summary>
        /// 切换到 TWRP Tab。
        /// </summary>
        [RelayCommand]
        private void ShowTwrp()
        {
            IsSystemFirmwareTab = false;
            RefreshResourceItems();
        }

        /// <summary>
        /// 从服务器拉取资源，当前仅保留页面入口。
        /// </summary>
        [RelayCommand]
        private void PullFromServer()
        {
        }

        /// <summary>
        /// 异步保存当前全部 ROM 与 TWRP 资源路径。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            IsSaving = true;
            try
            {
                await ResourceFilePathService.Instance.SaveAsync(CreateResourceFilePaths());
                Growl.Success(GetLangText("Message_ResourceSaveSucceeded"));
            }
            catch
            {
                Growl.Error(GetLangText("Message_ResourceSaveFailed"));
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// 关闭资源管理弹窗。
        /// </summary>
        [RelayCommand]
        private void CloseDialog()
        {
            Dialog?.Close();
        }

        /// <summary>
        /// 语言切换后刷新当前弹窗的资源行占位文本。
        /// </summary>
        /// <param name="sender">事件来源。</param>
        /// <param name="e">事件参数。</param>
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            foreach (var item in _systemFirmwareItems)
            {
                item.RefreshLanguage();
            }

            foreach (var item in _twrpItems)
            {
                item.RefreshLanguage();
            }
        }

        /// <summary>
        /// 根据当前 Tab 刷新弹窗展示的资源行。
        /// </summary>
        private void RefreshResourceItems()
        {
            ResourceItems.Clear();
            foreach (var item in IsSystemFirmwareTab ? _systemFirmwareItems : _twrpItems)
            {
                ResourceItems.Add(item);
            }
        }

        /// <summary>
        /// 判断当前是否允许保存资源路径。
        /// </summary>
        /// <returns>未处于保存状态时返回 true。</returns>
        private bool CanSave()
        {
            return !IsSaving;
        }

        /// <summary>
        /// 从启动时加载的资源路径缓存中回填两个 Tab 的文件路径。
        /// </summary>
        private void LoadSavedFilePaths()
        {
            ApplySavedFilePaths(_systemFirmwareItems, ResourceFilePathService.SystemFirmwareResourceType);
            ApplySavedFilePaths(_twrpItems, ResourceFilePathService.TwrpResourceType);
        }

        /// <summary>
        /// 将指定资源类型的缓存路径回填到系列文件行。
        /// </summary>
        /// <param name="items">需要回填的系列文件行集合。</param>
        /// <param name="resourceType">资源类型。</param>
        private static void ApplySavedFilePaths(IEnumerable<ResourceFileItemViewModel> items, string resourceType)
        {
            foreach (ResourceFileItemViewModel item in items)
            {
                item.FilePath = ResourceFilePathService.Instance.GetFilePath(resourceType, item.SeriesName);
            }
        }

        /// <summary>
        /// 将两个 Tab 中当前选择的文件行转换为待保存的资源路径模型。
        /// </summary>
        /// <returns>当前资源路径模型集合。</returns>
        private IEnumerable<ResourceFilePathModel> CreateResourceFilePaths()
        {
            DateTime savedAt = DateTime.Now;
            return CreateResourceFilePaths(_systemFirmwareItems, ResourceFilePathService.SystemFirmwareResourceType, savedAt)
                .Concat(CreateResourceFilePaths(_twrpItems, ResourceFilePathService.TwrpResourceType, savedAt));
        }

        /// <summary>
        /// 将单个 Tab 的文件行转换为资源路径模型。
        /// </summary>
        /// <param name="items">当前 Tab 的系列文件行集合。</param>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="savedAt">本次保存时间。</param>
        /// <returns>资源路径模型集合。</returns>
        private static IEnumerable<ResourceFilePathModel> CreateResourceFilePaths(
            IEnumerable<ResourceFileItemViewModel> items,
            string resourceType,
            DateTime savedAt)
        {
            return items.Select(item => new ResourceFilePathModel
            {
                ResourceType = resourceType,
                Series = item.SeriesName,
                FilePath = item.FilePath,
                UpdatedAt = savedAt
            });
        }

        /// <summary>
        /// 创建 S7、S8、S9 三个型号系列的资源文件行。
        /// </summary>
        /// <param name="fileFilterResourceKey">文件选择器过滤条件语言键。</param>
        /// <param name="s7PlaceholderResourceKey">S7 系列文件选择占位语言键。</param>
        /// <param name="s8PlaceholderResourceKey">S8 系列文件选择占位语言键。</param>
        /// <param name="s9PlaceholderResourceKey">S9 系列文件选择占位语言键。</param>
        /// <returns>资源文件行集合。</returns>
        private static ObservableCollection<ResourceFileItemViewModel> CreateResourceItems(
            string fileFilterResourceKey,
            string s7PlaceholderResourceKey,
            string s8PlaceholderResourceKey,
            string s9PlaceholderResourceKey)
        {
            string fileFilter = GetLangText(fileFilterResourceKey);
            return new ObservableCollection<ResourceFileItemViewModel>
            {
                new ResourceFileItemViewModel("S7系列", s7PlaceholderResourceKey, fileFilter),
                new ResourceFileItemViewModel("S8系列", s8PlaceholderResourceKey, fileFilter),
                new ResourceFileItemViewModel("S9系列", s9PlaceholderResourceKey, fileFilter)
            };
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
