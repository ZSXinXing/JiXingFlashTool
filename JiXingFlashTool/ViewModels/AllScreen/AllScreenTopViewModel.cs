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
    public class AllScreenTopViewModel : ObservableObject
    {
        public static string ASTShowOrHiddenSideBarMessageKey = "ASTShowOrHiddenSideBarMessageKey";
        public static string ASTSyncMouseChangeMessageKey = "ASTSyncMouseChangeMessageKey";
        public static string ASTSyncKeyboardChangeMessageKey = "ASTSyncKeyboardChangeMessageKey";
        public static string ASTSelectAllMessageKey = "ASTSelectAllMessageKey";
        public RelayCommand SettingCommand => new Lazy<RelayCommand>(() => new RelayCommand(Setting)).Value;
        public RelayCommand ShowSidebarCommand => new Lazy<RelayCommand>(() => new RelayCommand(ShowSidebar)).Value;

        public RelayCommand SyncMouseCommand => new Lazy<RelayCommand>(() => new RelayCommand(SyncMouse)).Value;
        public RelayCommand SyncKeyboardCommand => new Lazy<RelayCommand>(() => new RelayCommand(SyncKeyboard)).Value;
        public RelayCommand SelectAllCommand => new Lazy<RelayCommand>(() => new RelayCommand(SelectAll)).Value;

        private bool isShowSidebar = true;
        public bool IsShowSidebar { get => isShowSidebar; set => SetProperty(ref isShowSidebar, value); }

        private bool isSyncMouse = true;
        public bool IsSyncMouse { get => isSyncMouse; set => SetProperty(ref isSyncMouse, value); }

        private bool isSyncKeyboard = true;
        public bool IsSyncKeyboard { get => isSyncKeyboard; set => SetProperty(ref isSyncKeyboard, value); }

        private bool isSelectAll = false;
        public bool IsSelectAll { get => isSelectAll; set => SetProperty(ref isSelectAll, value); }
        private void Setting()
        {

            var vm = new CastScreenSettingViewModel();
            var dialog = new CastScreenSettingView();
            dialog.DataContext = vm;
            vm.Dialog = Dialog.Show(dialog);
        }

        private void ShowSidebar() => WeakReferenceMessenger.Default.Send<ValueChangedMessage<bool>, string>(new ValueChangedMessage<bool>(IsShowSidebar), ASTShowOrHiddenSideBarMessageKey);

        private void SyncMouse() => WeakReferenceMessenger.Default.Send<ValueChangedMessage<bool>, string>(new ValueChangedMessage<bool>(IsSyncMouse), ASTSyncMouseChangeMessageKey);

        private void SyncKeyboard() => WeakReferenceMessenger.Default.Send<ValueChangedMessage<bool>, string>(new ValueChangedMessage<bool>(IsSyncKeyboard), ASTSyncKeyboardChangeMessageKey);

        private void SelectAll() => WeakReferenceMessenger.Default.Send<ValueChangedMessage<bool>, string>(new ValueChangedMessage<bool>(IsSelectAll), ASTSelectAllMessageKey);
    }
}
