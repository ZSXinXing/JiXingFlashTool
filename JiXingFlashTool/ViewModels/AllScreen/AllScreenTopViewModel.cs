using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JiXingFlashTool.Views;
using System.Windows.Input;
using JiXingFlashTool.Views.AllScreen;

namespace JiXingFlashTool.ViewModels.AllScreen
{
    public partial class AllScreenTopViewModel : ObservableObject
    {
        public static string ASTShowOrHiddenSideBarMessageKey = "ASTShowOrHiddenSideBarMessageKey";
        public static string ASTSyncMouseChangeMessageKey = "ASTSyncMouseChangeMessageKey";
        public static string ASTSyncKeyboardChangeMessageKey = "ASTSyncKeyboardChangeMessageKey";
        public static string ASTSelectAllMessageKey = "ASTSelectAllMessageKey";

        /// <summary>
        /// 是否显示侧边栏。
        /// </summary>
        private bool isShowSidebar = true;

        /// <summary>
        /// 是否显示侧边栏。
        /// </summary>
        public bool IsShowSidebar
        {
            get => isShowSidebar;
            set => SetProperty(ref isShowSidebar, value);
        }

        /// <summary>
        /// 是否同步鼠标。
        /// </summary>
        private bool isSyncMouse = true;

        /// <summary>
        /// 是否同步鼠标。
        /// </summary>
        public bool IsSyncMouse
        {
            get => isSyncMouse;
            set => SetProperty(ref isSyncMouse, value);
        }

        /// <summary>
        /// 是否同步键盘。
        /// </summary>
        private bool isSyncKeyboard = true;

        /// <summary>
        /// 是否同步键盘。
        /// </summary>
        public bool IsSyncKeyboard
        {
            get => isSyncKeyboard;
            set => SetProperty(ref isSyncKeyboard, value);
        }

        /// <summary>
        /// 是否全选。
        /// </summary>
        private bool isSelectAll = false;

        /// <summary>
        /// 是否全选。
        /// </summary>
        public bool IsSelectAll
        {
            get => isSelectAll;
            set => SetProperty(ref isSelectAll, value);
        }

        /// <summary>
        /// 打开投屏设置弹窗命令。
        /// </summary>
        public RelayCommand SettingCommand => new Lazy<RelayCommand>(() => new RelayCommand(Setting)).Value;

        /// <summary>
        /// 切换侧边栏显示状态命令。
        /// </summary>
        public RelayCommand ShowSidebarCommand => new Lazy<RelayCommand>(() => new RelayCommand(ShowSidebar)).Value;

        /// <summary>
        /// 切换鼠标同步状态命令。
        /// </summary>
        public RelayCommand SyncMouseCommand => new Lazy<RelayCommand>(() => new RelayCommand(SyncMouse)).Value;

        /// <summary>
        /// 切换键盘同步状态命令。
        /// </summary>
        public RelayCommand SyncKeyboardCommand => new Lazy<RelayCommand>(() => new RelayCommand(SyncKeyboard)).Value;

        /// <summary>
        /// 切换全选状态命令。
        /// </summary>
        public RelayCommand SelectAllCommand => new Lazy<RelayCommand>(() => new RelayCommand(SelectAll)).Value;

        /// <summary>
        /// 打开投屏设置弹窗。
        /// </summary>
        private void Setting()
        {
            var vm = new CastScreenSettingViewModel();
            var dialog = new CastScreenSettingView();
            dialog.DataContext = vm;
            vm.Dialog = Dialog.Show(dialog);
        }

        /// <summary>
        /// 向群控列表发送侧边栏显示状态。
        /// </summary>
        private void ShowSidebar() => WeakReferenceMessenger.Default.Send<ValueChangedMessage<bool>, string>(new ValueChangedMessage<bool>(IsShowSidebar), ASTShowOrHiddenSideBarMessageKey);

        /// <summary>
        /// 向群控列表发送鼠标同步状态。
        /// </summary>
        private void SyncMouse() => WeakReferenceMessenger.Default.Send<ValueChangedMessage<bool>, string>(new ValueChangedMessage<bool>(IsSyncMouse), ASTSyncMouseChangeMessageKey);

        /// <summary>
        /// 向群控列表发送键盘同步状态。
        /// </summary>
        private void SyncKeyboard() => WeakReferenceMessenger.Default.Send<ValueChangedMessage<bool>, string>(new ValueChangedMessage<bool>(IsSyncKeyboard), ASTSyncKeyboardChangeMessageKey);

        /// <summary>
        /// 向群控列表发送全选状态。
        /// </summary>
        private void SelectAll() => WeakReferenceMessenger.Default.Send<ValueChangedMessage<bool>, string>(new ValueChangedMessage<bool>(IsSelectAll), ASTSelectAllMessageKey);
    }
}
