using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using JiXingFlashTool.Models;
using JiXingFlashTool.ObservableModel;
using JiXingFlashTool.Utils;
using JiXingFlashTool.Views;
using JiXingFlashTool.Properties;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using JiXingFlashTool.Model;

namespace JiXingFlashTool.ViewModels
{
    public class SyncFileViewModel : ObservableObject
    {
        private ObservableCollection<FileModel> fileCollection = new ObservableCollection<FileModel>();
        public ObservableCollection<FileModel> FileCollection { get => fileCollection; set => SetProperty(ref fileCollection, value); }

        public RelayCommand<FileModel> DeleteCommand => new Lazy<RelayCommand<FileModel>>(() => new RelayCommand<FileModel>(DeleteFile)).Value;
        public RelayCommand AddFileCommand => new Lazy<RelayCommand>(() => new RelayCommand(AddFile)).Value;
        public RelayCommand StartCommand => new Lazy<RelayCommand>(() => new RelayCommand(Start)).Value;
        public RelayCommand<FileModel> NeedInstallCommand => new Lazy<RelayCommand<FileModel>>(() => new RelayCommand<FileModel>(NeedInstall)).Value;
        public RelayCommand<FileModel> UsePhoneApkCommand => new Lazy<RelayCommand<FileModel>>(() => new RelayCommand<FileModel>(UsePhoneApk)).Value;
        public List<DeviceModel> NeedInstallDeviceList;
        public Dialog Dialog { get; set; }


        private void AddFile() {
            using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog())
            {
                ofd.Filter = "文件|*.*";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string[] files = ofd.FileNames;

                    ThreadPool.QueueUserWorkItem((x) =>
                    {
                        List<FileModel> fileList = fileCollection.ToList();
                        foreach (string path in files)
                        {

                            //如果路径已经存在就不添加
                            if (fileList.Exists(it =>
                            {
                                if (it == null) return false;
                                return it.Equals(path);
                            })) continue;
                            FileModel fileModel = new FileModel();
                            FileInfo file = new FileInfo(path);
                            fileModel.Size = CommonTool.GetSizeString(file.Length);
                            fileModel.Extension = file.Extension;
                            fileModel.Name = file.Name.ToString();
                            fileModel.FileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(path);
                            fileModel.Path = path;

                            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                            SynchronizationContext.Current.Post(pl =>
                            {
                                FileCollection.Add(fileModel);
                            }, null);
                        }
                    });
                }
            }
        }

        private void DeleteFile(FileModel model) {
            List<FileModel> fileList = fileCollection.ToList();
            int findIndex = -1;
            findIndex = fileList.FindIndex(it =>
            {
                if (it == null) return false;
                return it.Path.Equals(model.Path);
            });

            if (findIndex != -1) { 
                fileCollection.RemoveAt(findIndex);
            }
        }

        private void UsePhoneApk(FileModel model) { 
            
        }

        private void NeedInstall(FileModel model)
        {
        }

        private void Start() {

            if (fileCollection.ToList().Count == 0)
            {
                Growl.Warning("请先选择安装包");
                return;
            }

            var vm = new SyncFileProgressViewModel();
            vm.FileList = this.fileCollection.ToList();
            vm.DeviceList = this.NeedInstallDeviceList;
            var dialog = new SyncFileProgressView();
            dialog.DataContext = vm;
            this.Dialog.Close();
            vm.Dialog = Dialog.Show(dialog);
            vm.Start();
        }

    }
}
