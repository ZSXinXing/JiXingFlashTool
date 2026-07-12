using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using JiXingFlashTool.Models;
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.EventArg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace JiXingFlashTool.ViewModels.AllScreen
{
    /// <summary>
    /// 群控左侧设备列表视图模型，负责侧边栏宽度、设备集合和设备点击交互。
    /// </summary>
    public partial class AllScreenLeftViewModel : ObservableObject
    {
        /// <summary>
        /// 显示或隐藏侧边栏的消息键。
        /// </summary>
        public static string ASLShowCastScreenMessageKey = "ASLShowCastScreenMessageKey";

        /// <summary>
        /// 侧边栏宽度。
        /// </summary>
        private int sidebarWidth = 200;
        /// <summary>
        /// 侧边栏宽度。
        /// </summary>
        public int SidebarWidth
        {
            get => sidebarWidth;
            set => SetProperty(ref sidebarWidth, value);
        }

        /// <summary>
        /// 当前群控设备集合。
        /// </summary>
        private ObservableCollection<DeviceItemViewModel> deviceCollection = new ObservableCollection<DeviceItemViewModel>();
        /// <summary>
        /// 当前群控设备集合。
        /// </summary>
        public ObservableCollection<DeviceItemViewModel> DeviceCollection
        {
            get => deviceCollection;
            set => SetProperty(ref deviceCollection, value);
        }

        /// <summary>
        /// 群控设备列表内部缓存。
        /// </summary>
        private List<DeviceItemViewModel> deviceList = new List<DeviceItemViewModel>();
        /// <summary>
        /// 当前群控设备列表内部缓存。
        /// </summary>
        public List<DeviceItemViewModel> DeviceList
        {
            get => deviceList;
            set => SetProperty(ref deviceList, value);
        }

        /// <summary>
        /// 双击设备项命令。
        /// </summary>
        [RelayCommand]
        private void ItemDoubleClick(DeviceItemViewModel ob)
        {
            SynchronizationContext.Current.Post(pl =>
            {
                ob.IsSelect = true;
                WeakReferenceMessenger.Default.Send<ValueChangedMessage<DeviceItemViewModel>, string>
                (new ValueChangedMessage<DeviceItemViewModel>(ob), ASLShowCastScreenMessageKey);
            }, null);
        }

        /// <summary>
        /// 单击设备项命令。
        /// </summary>
        [RelayCommand]
        private void ItemClick(DeviceItemViewModel ob)
        {
            SynchronizationContext.Current.Post(pl =>
            {
                ob.IsSelect = !ob.IsSelect;
            }, null);
        }

        /// <summary>
        /// 初始化群控左侧列表视图模型。
        /// </summary>
        public AllScreenLeftViewModel()
        {
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<bool>, string>(this, AllScreenTopViewModel.ASTShowOrHiddenSideBarMessageKey, ShowOrHiddenSideBar);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<CastScreenItemViewModel>, string>(this, DeviceScreenListViewModel.DSCastScreenConnectMessageKey, CastScreenConnect);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<CastScreenItemViewModel>, string>(this, DeviceScreenListViewModel.DSCastScreenDisconnectMessageKey, CastScreenDisconnect);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<bool>, string>(this, DeviceScreenListViewModel.DSCastScreenSelectMessageKey, SelectAll);
        }

        /// <summary>
        /// 读取初始化数据。
        /// </summary>
        public void ViewLoad()
        {
        }

        /// <summary>
        /// 侧边栏显示或隐藏。
        /// </summary>
        private void ShowOrHiddenSideBar(object recipient, ValueChangedMessage<bool> e)
        {
            SidebarWidth = e.Value ? 200 : 0;
        }

        /// <summary>
        /// 投屏设备连接后加入列表。
        /// </summary>
        private void CastScreenConnect(object recipient, ValueChangedMessage<CastScreenItemViewModel> e)
        {
            try
            {
                int insertIndex = DeviceList.Count;
                DeviceList.Insert(insertIndex, e.Value);
                DeviceCollection.Insert(insertIndex, e.Value);
            }
            catch
            {
                try
                {
                    DeviceList.Add(e.Value);
                    DeviceCollection.Add(e.Value);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 投屏设备断开后从列表移除。
        /// </summary>
        private void CastScreenDisconnect(object recipient, ValueChangedMessage<CastScreenItemViewModel> e)
        {
            try
            {
                DeviceList.Remove(e.Value);
                DeviceCollection.Remove(e.Value);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 同步全选状态到所有设备项。
        /// </summary>
        private void SelectAll(object recipient, ValueChangedMessage<bool> e)
        {
            try
            {
                foreach (var ob in DeviceList)
                {
                    ob.IsSelect = e.Value;
                }
            }
            catch
            {
            }
        }
    }
}
