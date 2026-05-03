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
using JiXingFlashTool.ObservableModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.Models;
using JiXingFlashTool.Enums;
using JiXingFlashTool.Delegate;
using JiXingFlashTool.Properties;

namespace JiXingFlashTool.ViewModels
{
    public class AdbListViewModel : ObservableObject
    {
        public List<DeviceObservableModel> SelectDeviceList { get; set; } = new List<DeviceObservableModel>();

        protected ObservableCollection<AdbCommandModel> commandCollection = new ObservableCollection<AdbCommandModel>();
        public ObservableCollection<AdbCommandModel> CommandCollection { get => commandCollection; set => SetProperty(ref commandCollection, value); }

        public RelayCommand<AdbCommandModel> AdbCommand => new Lazy<RelayCommand<AdbCommandModel>>(() => new RelayCommand<AdbCommandModel>(AdbCommandExecute)).Value;
        public RelayCommand ExecuteCustomCommand => new Lazy<RelayCommand>(() => new RelayCommand(ExecuteCustom)).Value;
        public CommonFinishDelegate FinishDelegate { get; set; }
        public string CustomCommandTxt { get; set; }
        public Dialog dialog { get; set; }
        private ShowCommandType ShowCommandType;

        public AdbListViewModel() {
            InitCommand();
        }

        private void AdbCommandExecute(AdbCommandModel model) => ExecuteAdbCommand(model);

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
