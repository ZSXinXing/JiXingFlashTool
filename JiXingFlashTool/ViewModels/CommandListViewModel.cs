using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.Delegate;
using JiXingFlashTool.Enums;
using JiXingFlashTool.Extensions;
using JiXingFlashTool.Model;
using JiXingFlashTool.Models;
using JiXingFlashTool.ObservableModel;
using JiXingFlashTool.Views;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiXingFlashTool.ViewModels
{
    public class CommandListViewModel : ObservableObject
    {
        public List<DeviceObservableModel> SelectDeviceList { get; set; } = new List<DeviceObservableModel>();

        protected ObservableCollection<CommandModel> commandCollection = new ObservableCollection<CommandModel>();
        public ObservableCollection<CommandModel> CommandCollection { get => commandCollection; set => SetProperty(ref commandCollection, value); }
        public RelayCommand<CommandModel> ExecuteCommand => new Lazy<RelayCommand<CommandModel>>(() => new RelayCommand<CommandModel>(Execute)).Value;
        public CommonFinishDelegate FinishDelegate { get; set; }
        public Dialog Dialog { get; set; }

        public CommandListViewModel() {
            InitCommand();
        }

        private void InitCommand()
        {
            commandCollection.Add(new CommandModel("重启到系统",TWRPCommandType.RebootSystem));
            commandCollection.Add(new CommandModel("重启到TWRP", TWRPCommandType.RebootTWRP));
            commandCollection.Add(new CommandModel("双清", TWRPCommandType.Wipe));
            commandCollection.Add(new CommandModel("清除系统", TWRPCommandType.ClearSystem));
            commandCollection.Add(new CommandModel("格式化", TWRPCommandType.Format));
            commandCollection.Add(new CommandModel("开启侧载", TWRPCommandType.Sideload));
            commandCollection.Add(new CommandModel("刷入文件", TWRPCommandType.FlashFile) { NeedSelectFile = true }); ;
            commandCollection.Add(new CommandModel("更新内核", TWRPCommandType.FlashKernel) { NeedSelectFile = true });
            commandCollection.Add(new CommandModel("更新TWRP", TWRPCommandType.UpdateTWRP) { NeedSelectFile = true,FileFilter= "TWRP文件 (*.img)|*.img" });
            commandCollection.Add(new CommandModel("解密", TWRPCommandType.Decryption) { NeedSelectFile = false });
        }

        public static void Show(List<DeviceObservableModel> deviceList, CommonFinishDelegate finishDelegate)
        {
            var vm = new CommandListViewModel();
            vm.SelectDeviceList = deviceList;
            var view = new CommandListView();
            view.DataContext = vm;
            vm.Dialog = Dialog.Show(view);
            vm.FinishDelegate = finishDelegate;
        }

        private void Execute(CommandModel model) {

            if (model.NeedSelectFile)
            {

                using (System.Windows.Forms.OpenFileDialog ofd = new OpenFileDialog())
                {
                    if (model.FileFilter.IsNull()) ofd.Filter = "文件|*.*";
                    else ofd.Filter = model.FileFilter;
                    ofd.Multiselect = false;
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        model.FilePath = ofd.FileName;
                        if (FinishDelegate != null) this.FinishDelegate(model);
                        this.Dialog.Close();
                    }
                }
            }
            else {
                if (FinishDelegate != null) this.FinishDelegate(model);
                this.Dialog.Close();
            }
        }
    }
}
