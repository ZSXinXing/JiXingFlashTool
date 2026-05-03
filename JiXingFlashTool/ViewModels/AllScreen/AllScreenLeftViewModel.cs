using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using JXAdbCore.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using JiXingFlashTool.EventArg;
using JiXingFlashTool.Extensions;
using JiXingFlashTool.Models;
using JiXingFlashTool.ObservableModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.Model;

namespace JiXingFlashTool.ViewModels.AllScreen
{
    public class AllScreenLeftViewModel : ObservableObject
    {
        public static string ASLShowCastScreenMessageKey = "ASLShowCastScreenMessageKey";

        private int sidebarWidth = 200;
        public int SidebarWidth { get => sidebarWidth; set => SetProperty(ref sidebarWidth, value); }
        private ObservableCollection<DeviceObservableModel> deviceCollection = new ObservableCollection<DeviceObservableModel>();
        public ObservableCollection<DeviceObservableModel> DeviceCollection { get => deviceCollection; set => SetProperty(ref deviceCollection, value); }
        private List<DeviceObservableModel> DeviceList = new List<DeviceObservableModel>();

        public RelayCommand<DeviceObservableModel> ItemDoubleClickCommand => new Lazy<RelayCommand<DeviceObservableModel>>(() => new RelayCommand<DeviceObservableModel>(ItemDoubleClick)).Value;
        public RelayCommand<DeviceObservableModel> ItemClickCommand => new Lazy<RelayCommand<DeviceObservableModel>>(() => new RelayCommand<DeviceObservableModel>(ItemClick)).Value;
        public AllScreenLeftViewModel()
        {

            WeakReferenceMessenger.Default.Register<ValueChangedMessage<bool>, string>(this, AllScreenTopViewModel.ASTShowOrHiddenSideBarMessageKey, ShowOrHiddenSideBar);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<ObservableCastScreenModel>, string>(this, DeviceScreenListViewModel.DSCastScreenConnectMessageKey, CastScreenConnect);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<ObservableCastScreenModel>, string>
                (this, DeviceScreenListViewModel.DSCastScreenDisconnectMessageKey, CastScreenDisconnect);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<bool>, string>
                (this, DeviceScreenListViewModel.DSCastScreenSelectMessageKey, SelectAll);
        }

        //读取
        public void ViewLoad()
        {
        }


        private void OnDeviceRemarkChanage(object sender, DeviceEventArgs e)
        {
            DeviceModel device = e.DeviceModel;
            if (device != null)
            {
                try
                {
                    DeviceObservableModel ob = DeviceList.Find(it =>
                    {
                        if (it == null) return false;
                        return it.Device.RoSerialNo.Equals(device.RoSerialNo);
                    });
                }
                catch { }
            }
        }

        private void ShowOrHiddenSideBar(object recipient, ValueChangedMessage<bool> e)
        {
            if (e.Value) SidebarWidth = 200;
            else SidebarWidth = 0;
        }

        private void CastScreenConnect(object recipient, ValueChangedMessage<ObservableCastScreenModel> e)
        {
            try
            {

                int insertIndex = DeviceList.Count;
                try
                {
                    DeviceList.Insert(insertIndex, e.Value);
                    DeviceCollection.Insert(insertIndex, e.Value);
                }
                catch
                {
                    DeviceList.Add(e.Value);
                    DeviceCollection.Add(e.Value);
                }
            }
            catch { }
        }

        private void CastScreenDisconnect(object recipient, ValueChangedMessage<ObservableCastScreenModel> e)
        {
            try
            {
                DeviceList.Remove(e.Value);
                DeviceCollection.Remove(e.Value);
            }
            catch { }
        }

        private void SelectAll(object recipient, ValueChangedMessage<bool> e)
        {
            try
            {
                foreach (var ob in DeviceList)
                {
                    ob.RefreshItemBackgroundColor();
                    ob.RefreshItemForegroundColor();
                    //  ob.RefreshItemBorderThickness();
                }
            }
            catch { }
        }

        private void ItemDoubleClick(DeviceObservableModel ob)
        {
            SynchronizationContext.Current.Post(pl =>
            {
                ob.IsSelect = true;
                ob.RefreshItemBackgroundColor();
                ob.RefreshItemForegroundColor();
                ob.RefreshItemBorderThickness();
                WeakReferenceMessenger.Default.Send<ValueChangedMessage<DeviceObservableModel>, string>
                (new ValueChangedMessage<DeviceObservableModel>(ob), ASLShowCastScreenMessageKey);
            }, null);
        }

        private void ItemClick(DeviceObservableModel ob)
        {
            SynchronizationContext.Current.Post(pl =>
            {
                ob.IsSelect = !ob.IsSelect;
                ob.RefreshItemBackgroundColor();
                ob.RefreshItemForegroundColor();
                ob.RefreshItemBorderThickness();
            }, null);
        }


    }
}
