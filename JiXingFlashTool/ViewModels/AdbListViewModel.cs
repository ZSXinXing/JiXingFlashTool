using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.Models;
using JiXingFlashTool.Enums;
using JiXingFlashTool.Delegate;
using JiXingFlashTool.Properties;

namespace JiXingFlashTool.ViewModels
{
    public class AdbListViewModel : ObservableObject
    {
        private List<DeviceItemViewModel> selectDeviceList = new List<DeviceItemViewModel>();
        /// <summary>
        /// 当前选中的设备列表。
        /// </summary>
        public List<DeviceItemViewModel> SelectDeviceList
        {
            get => selectDeviceList;
            set => SetProperty(ref selectDeviceList, value);
        }

        private ObservableCollection<AdbCommandModel> commandCollection = new ObservableCollection<AdbCommandModel>();
        /// <summary>
        /// 常用 ADB 指令集合。
        /// </summary>
        public ObservableCollection<AdbCommandModel> CommandCollection
        {
            get => commandCollection;
            set => SetProperty(ref commandCollection, value);
        }

        private CommonFinishDelegate finishDelegate;
        /// <summary>
        /// 指令执行完成后的回调。
        /// </summary>
        public CommonFinishDelegate FinishDelegate
        {
            get => finishDelegate;
            set => SetProperty(ref finishDelegate, value);
        }

        private string customCommandTxt;
        /// <summary>
        /// 自定义指令输入内容。
        /// </summary>
        public string CustomCommandTxt
        {
            get => customCommandTxt;
            set => SetProperty(ref customCommandTxt, value);
        }

        private Dialog dialog;
        /// <summary>
        /// 当前弹窗实例。
        /// </summary>
        public Dialog Dialog
        {
            get => dialog;
            set => SetProperty(ref dialog, value);
        }

        private ShowCommandType showCommandType;

        public AdbListViewModel() {
            InitCommand();
        }

        /// <summary>
        /// 执行指令命令。
        /// </summary>
        public RelayCommand<AdbCommandModel> AdbCommand => new Lazy<RelayCommand<AdbCommandModel>>(() => new RelayCommand<AdbCommandModel>(Adb)).Value;

        /// <summary>
        /// 执行自定义指令命令。
        /// </summary>
        public RelayCommand ExecuteCustomCommand => new Lazy<RelayCommand>(() => new RelayCommand(ExecuteCustom)).Value;

        /// <summary>
        /// 执行常用指令。
        /// </summary>
        private void Adb(AdbCommandModel model) => ExecuteAdbCommand(model);

        /// <summary>
        /// 执行自定义指令。
        /// </summary>
        private void ExecuteCustom() => ExecuteAdbCommand(new AdbCommandModel("自定义指令", CustomCommandTxt) { IsCustom = true});

        private void ExecuteAdbCommand(AdbCommandModel model) {

            if (FinishDelegate != null) FinishDelegate(model);
            this.dialog.Close();
        }

        private void InitCommand()
        {
            CommandCollection.Add(new AdbCommandModel("重启系统", "reboot"));
            CommandCollection.Add(new AdbCommandModel("进入TWRP", "reboot recovery"));
            CommandCollection.Add(new AdbCommandModel("进入挖煤", "reboot download"));
            CommandCollection.Add(new AdbCommandModel("打开手电筒", "\"echo 1 > /sys/class/camera/flash/rear_flash\"") { NeedRoot=true});
            CommandCollection.Add(new AdbCommandModel("关闭手电筒", "\"echo 0 > /sys/class/camera/flash/rear_flash\"") { NeedRoot = true });
            CommandCollection.Add(new AdbCommandModel("关闭开发者", "settings put global development_settings_enabled 0"));
            CommandCollection.Add(new AdbCommandModel("跳过向导", "settings put secure user_setup_complete 1 && settings put global device_provisioned 1"));
        }

    }
}

