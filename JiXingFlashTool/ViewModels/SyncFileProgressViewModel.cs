using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shapes;
using System.Windows.Threading;
using JiXingFlashTool.Models;
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.Utils;
using JiXingFlashTool.Properties;
using JiXingFlashTool.Model;

namespace JiXingFlashTool.ViewModels
{
    public partial class SyncFileProgressViewModel : ObservableObject
    {
        /// <summary>
        /// 是否完成全部同步。
        /// </summary>
        private bool finishSync = false;

        /// <summary>
        /// 是否完成全部同步。
        /// </summary>
        public bool FinishSync
        {
            get => finishSync;
            set => SetProperty(ref finishSync, value);
        }

        private int finishCount = 0;

        /// <summary>
        /// 设备进度集合。
        /// </summary>
        private ObservableCollection<DeviceInstallItemViewModel> deviceCollection = new ObservableCollection<DeviceInstallItemViewModel>();

        /// <summary>
        /// 设备进度集合。
        /// </summary>
        public ObservableCollection<DeviceInstallItemViewModel> DeviceCollection
        {
            get => deviceCollection;
            set => SetProperty(ref deviceCollection, value);
        }

        /// <summary>
        /// 当前弹窗实例。
        /// </summary>
        private Dialog dialog;

        /// <summary>
        /// 当前弹窗实例。
        /// </summary>
        public Dialog Dialog
        {
            get => dialog;
            set => SetProperty(ref dialog, value);
        }

        /// <summary>
        /// 当前设备列表。
        /// </summary>
        private List<DeviceModel> deviceList;

        /// <summary>
        /// 当前设备列表。
        /// </summary>
        public List<DeviceModel> DeviceList
        {
            get => deviceList;
            set => SetProperty(ref deviceList, value);
        }

        /// <summary>
        /// 当前文件列表。
        /// </summary>
        private List<FileModel> fileList;

        /// <summary>
        /// 当前文件列表。
        /// </summary>
        public List<FileModel> FileList
        {
            get => fileList;
            set => SetProperty(ref fileList, value);
        }

        private List<DeviceInstallItemViewModel> deviceInstallModels = new List<DeviceInstallItemViewModel>();

        private Thread syncFileThread;
        private List<Thread> AllSyncFileThread = new List<Thread>();
        private List<Thread> AllInstallThread = new List<Thread>();
        [RelayCommand]
        public void Start() {

            //组装数组
            for (int i = 0; i < DeviceList.Count; i++)
            {
                DeviceInstallModel deviceInstall = new DeviceInstallModel() { Device = DeviceList[i], FileList = FileList, SumCount = FileList.Count };
                DeviceInstallItemViewModel ob = new DeviceInstallItemViewModel(deviceInstall);

                DeviceCollection.Add(ob);
                deviceInstallModels.Add(ob);
            }

            syncFileThread = new Thread(async () =>
            {
                for (int i = 0; i < deviceInstallModels.Count; i++)
                {
                    DeviceInstallItemViewModel ob = deviceInstallModels[i];
                    for (int j = 0; j < ob.DeviceInstall.FileList.Count; j++)
                    {
                        FileModel fileModel = ob.DeviceInstall.FileList[j];

                        if (fileModel.IsApk && fileModel.NeedInstall)
                        {
                            InstallTask(ob, fileModel);
                        }
                        else
                        {
                            PushTask(ob, fileModel.Path);
                        }
                    }
                }
            });
            syncFileThread.Start();
        }

        private void InstallTask(DeviceInstallItemViewModel ob,FileModel fileInfo) {
            Thread thread = new Thread(() =>
            {
                using (Stream apkStream = File.OpenRead(fileInfo.Path))
                {
                    try
                    {
                        //创建文件夹
                        AdbService.Instance.CreateFolder(ob.DeviceInstall.Device, StaticConstant.PhoneSyncFolder);

                        string remotePath = string.Format("{0}{1}", StaticConstant.PhoneSyncFolder, System.IO.Path.GetFileName(fileInfo.Path));

                        //如果优先从手机安卓,需要拼单是否存在文件
                        if (fileInfo.UsePhoneApk)
                        {
                            if (!AdbService.Instance.FileExist(ob.DeviceInstall.Device, remotePath))
                            {
                                Debug.WriteLine($"{ob.IP} {fileInfo.Name} 安装包不存在,需要传输");
                                AdbService.Instance.Push(ob.DeviceInstall.Device, apkStream, remotePath);
                            }
                            else
                            {
                                Debug.WriteLine($"{ob.IP} {fileInfo.Name} 安装包存在,不需要传输");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"{ob.IP} {fileInfo.Name} 不使用手机包,需要传输");
                            AdbService.Instance.Push(ob.DeviceInstall.Device, apkStream, remotePath);
                        }
                        
                        AdbService.Instance.InstallFromPhone(ob.DeviceInstall.Device, remotePath);

                        ob.SuccessCount = ob.SuccessCount +1;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        ob.FailureCount = ob.FailureCount + 1;
                    }
                    ob.Progress = ob.Progress + 1;
                    CheckProgress(ob.Progress == (ob.DeviceInstall.FileList.Count));
                }

            });
            thread.Start();
            AllInstallThread.Add(thread);
        }


        private void PushTask(DeviceInstallItemViewModel ob, string path)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    //创建文件夹
                    AdbService.Instance.CreateFolder(ob.DeviceInstall.Device, StaticConstant.PhoneSyncFolder);
                    string remotePath = string.Format("{0}{1}", StaticConstant.PhoneSyncFolder, System.IO.Path.GetFileName(path));
                    //推送文件
                    AdbService.Instance.Push(ob.DeviceInstall.Device, path, remotePath);
                    //通知广播
                    AdbService.Instance.RefreshFile(ob.DeviceInstall.Device, remotePath);
                    ob.SuccessCount = ob.SuccessCount + 1;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    ob.FailureCount = ob.FailureCount + 1;
                }
                ob.Progress = ob.Progress + 1;
                CheckProgress(ob.Progress == (ob.DeviceInstall.FileList.Count));
            });
            thread.Start();
            AllSyncFileThread.Add(thread);
        }
        [RelayCommand]
        private void Close(){

            foreach (Thread thread in AllInstallThread) {
                if (thread != null)
                {
                    thread.Abort();
                }
            }

            this.Dialog.Close();
        }

        [RelayCommand]
        private void Finish() {
            Close();
        }

        private void CheckProgress(bool finish) {
            if (finish) finishCount++;
            if (finishCount >= deviceInstallModels.Count) {
                FinishSync = true;
            }
        }
    }
}

