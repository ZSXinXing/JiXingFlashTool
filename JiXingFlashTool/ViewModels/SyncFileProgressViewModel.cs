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
using JiXingFlashTool.ObservableModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.Utils;
using JiXingFlashTool.Properties;
using JiXingFlashTool.Model;

namespace JiXingFlashTool.ViewModels
{
    public class SyncFileProgressViewModel : ObservableObject
    {
        private bool finishSync = false;
        public bool FinishSync { get => finishSync; set => SetProperty(ref finishSync, value); }
        private int FinishCount = 0;

        private ObservableCollection<ObservableDeviceInstallModel> deviceCollection = new ObservableCollection<ObservableDeviceInstallModel>();
        public ObservableCollection<ObservableDeviceInstallModel> DeviceCollection { get => deviceCollection; set => SetProperty(ref deviceCollection, value); }

        public RelayCommand CloseCommand => new Lazy<RelayCommand>(() => new RelayCommand(Close)).Value;
        public RelayCommand FinishCommand => new Lazy<RelayCommand>(() => new RelayCommand(Finish)).Value;
        
        public Dialog Dialog { get; set; }

        public List<DeviceModel> DeviceList { get; set; }
        public List<FileModel> FileList { get; set; }

        private List<ObservableDeviceInstallModel> deviceInstallModels = new List<ObservableDeviceInstallModel>();

        private Thread syncFileThread;
        private List<Thread> AllSyncFileThread = new List<Thread>();
        private List<Thread> AllInstallThread = new List<Thread>();
        private int MaxInstallThreadCount = 20;
        private int CurrentThreadCount = 0;
        public void Start() {

            //组装数组
            for (int i = 0; i < DeviceList.Count; i++)
            {
                DeviceInstallModel deviceInstall = new DeviceInstallModel() { Device = DeviceList[i], FileList = FileList, SumCount = FileList.Count };
                ObservableDeviceInstallModel ob = new ObservableDeviceInstallModel(deviceInstall);

                DeviceCollection.Add(ob);
                deviceInstallModels.Add(ob);
            }

            syncFileThread = new Thread(async () =>
            {
                for (int i = 0; i < deviceInstallModels.Count; i++)
                {
                    ObservableDeviceInstallModel ob = deviceInstallModels[i];
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

        private void InstallTask(ObservableDeviceInstallModel ob,FileModel fileInfo) {
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
                        ob.FailureCount = ob.FailureCount + 1;
                    }
                    ob.Progress = ob.Progress + 1;
                    CheckProgress(ob.Progress == (ob.DeviceInstall.FileList.Count));
                }

            });
            thread.Start();
            AllInstallThread.Add(thread);
        }


        private void PushTask(ObservableDeviceInstallModel ob, string path)
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
                    ob.FailureCount = ob.FailureCount + 1;
                }
                ob.Progress = ob.Progress + 1;
                CheckProgress(ob.Progress == (ob.DeviceInstall.FileList.Count));
            });
            thread.Start();
            AllSyncFileThread.Add(thread);
        }
        private void Close(){

            foreach (Thread thread in AllInstallThread) {
                if (thread != null)
                {
                    thread.Abort();
                }
            }

            this.Dialog.Close();
        }

        private void Finish() {
            Close();
        }

        private void CheckProgress(bool finish) {
            if (finish) FinishCount++;
            if (FinishCount >= deviceInstallModels.Count) {
                FinishSync = true;
            }
        }
    }
}
