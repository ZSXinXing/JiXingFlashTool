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
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Services;
using static System.Windows.Forms.LinkLabel;

namespace JiXingFlashTool.ViewModels.AllScreen
{
    public class AllScreenBottomViewModel : ObservableObject
    {
        #region Message Key
        public static string ASBVMScreenSizeChanageMessageKey = "ASBVMScreenSizeChanageMessageKey";
        #endregion

        private int screenCurrentWidth;
        public int ScreenCurrentWidth {
            get { 
                return screenCurrentWidth;
            }
            set => SetProperty(ref screenCurrentWidth, value);
        }

        private int screenMiniWidth;
        public int ScreenMiniWidth
        {
            get
            {
                return screenMiniWidth;
            }
            set => SetProperty(ref screenMiniWidth, value);
        }

        private int screenMaxWidth;
        public int ScreenMaxWidth
        {
            get
            {
                return screenMaxWidth;
            }
            set => SetProperty(ref screenMaxWidth, value);
        }

        private int deviceCount = 0;
        public int DeviceCount
        {
            get
            {
                return deviceCount;
            }
            set => SetProperty(ref deviceCount, value);
        }

        private int castScreenCount = 0;
        public int CastScreenCount
        {
            get
            {
                return castScreenCount;
            }
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

