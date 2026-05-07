using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using JiXingFlashTool.EventArg;
using JiXingFlashTool.Services;
using static System.Windows.Forms.LinkLabel;

namespace JiXingFlashTool.ViewModels.AllScreen
{
    public class AllScreenBottomViewModel : ObservableObject
    {
        #region Message Key
        public static string ASBVMScreenSizeChanageMessageKey = "ASBVMScreenSizeChanageMessageKey";
        #endregion

        /// <summary>
        /// 当前投屏条的宽度。
        /// </summary>
        private int screenCurrentWidth;
        /// <summary>
        /// 当前投屏条的宽度。
        /// </summary>
        public int ScreenCurrentWidth
        {
            get => screenCurrentWidth;
            set => SetProperty(ref screenCurrentWidth, value);
        }

        /// <summary>
        /// 投屏条允许的最小宽度。
        /// </summary>
        private int screenMiniWidth;
        /// <summary>
        /// 投屏条允许的最小宽度。
        /// </summary>
        public int ScreenMiniWidth
        {
            get => screenMiniWidth;
            set => SetProperty(ref screenMiniWidth, value);
        }

        /// <summary>
        /// 投屏条允许的最大宽度。
        /// </summary>
        private int screenMaxWidth;
        /// <summary>
        /// 投屏条允许的最大宽度。
        /// </summary>
        public int ScreenMaxWidth
        {
            get => screenMaxWidth;
            set => SetProperty(ref screenMaxWidth, value);
        }

        /// <summary>
        /// 当前连接设备数量。
        /// </summary>
        private int deviceCount;
        /// <summary>
        /// 当前连接设备数量。
        /// </summary>
        public int DeviceCount
        {
            get => deviceCount;
            set => SetProperty(ref deviceCount, value);
        }

        /// <summary>
        /// 当前投屏设备数量。
        /// </summary>
        private int castScreenCount;
        /// <summary>
        /// 当前投屏设备数量。
        /// </summary>
        public int CastScreenCount
        {
            get => castScreenCount;
            set => SetProperty(ref castScreenCount, value);
        }

        public AllScreenBottomViewModel() {
            DeviceService.Instance.DeviceDisconnected += PDeviceDisconnected;
            DeviceService.Instance.DeviceConnected += PDeviceConnected;
            DeviceService.Instance.DeviceChange += DeviceChange;

            WeakReferenceMessenger.Default.Register<ValueChangedMessage<int>, string>(this,DeviceScreenListViewModel.DSCastScreenCountChangeMessageKey, CastScreenCountChange);


            WeakReferenceMessenger.Default.Register<ValueChangedMessage<bool>, string>(this, DeviceScreenWindowModel.DSCloseSceenWindowMessageKey, CloseWindow);
        }

        public void ViewLoad() {

            ScreenMaxWidth = AppService.Instance.AppConfig.ScreenMaxWidth;
            ScreenCurrentWidth = AppService.Instance.AppConfig.ScreenCurrentWidth;
            ScreenMiniWidth = AppService.Instance.AppConfig.ScreenMiniWidth;

            ShowDeviceCount();
        }

        public void ValueChange(double value)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) {
                WeakReferenceMessenger.Default.Send<ValueChangedMessage<double>, string>(new ValueChangedMessage<double>(value), ASBVMScreenSizeChanageMessageKey);
            }
        }

        private void PDeviceConnected(object sender, EventArg.DeviceEventArgs e)
        {
            ShowDeviceCount();
        }

        private void PDeviceDisconnected(object sender, EventArg.DeviceEventArgs e)
        {
            ShowDeviceCount();
        }

        private void DeviceChange(object sender, DeviceEventArgs e)
        {
            ShowDeviceCount();
        }

        private void CastScreenCountChange(object recipient, ValueChangedMessage<int> e) {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SynchronizationContext.SetSynchronizationContext(new
                    DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(pl =>
                {
                    CastScreenCount = e.Value;
                }, null);
            });
        }

        private void ShowDeviceCount() {
            try
            {
                if (System.Windows.Application.Current.Dispatcher == null) return;

                ThreadPool.QueueUserWorkItem(delegate
                {
                    SynchronizationContext.SetSynchronizationContext(new
                        DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                    SynchronizationContext.Current.Post(pl =>
                    {
                        DeviceCount = DeviceService.Instance.DeviceList.Count;
                    }, null);
                });
            }
            catch { 
            }
        }

        private void CloseWindow(object recipient, ValueChangedMessage<bool> e)
        {
            DeviceService.Instance.DeviceDisconnected += PDeviceDisconnected;
            DeviceService.Instance.DeviceConnected += PDeviceConnected;
            DeviceService.Instance.DeviceChange += DeviceChange;
        }
    }
}

