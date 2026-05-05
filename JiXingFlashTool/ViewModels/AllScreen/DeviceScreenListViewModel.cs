using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using JXAdbCore.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using JiXingFlashTool.Enums;
using JiXingFlashTool.EventArg;
using JiXingFlashTool.Extensions;
using JiXingFlashTool.Models;
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.Utils;
using static System.Windows.Forms.LinkLabel;
using AndroidKeycode = JiXingFlashTool.Enums.AndroidKeycode;
using JiXingFlashTool.Model;

namespace JiXingFlashTool.ViewModels.AllScreen
{
    public class DeviceScreenListViewModel : ObservableObject, IDisposable
    {

        public static string DSCastScreenConnectMessageKey = "DSCastScreenConnectMessageKey";
        public static string DSCastScreenDisconnectMessageKey = "DSCastScreenDisconnectMessageKey";
        public static string DSCastScreenSelectMessageKey = "DSCastScreenSelectMessageKey";
        public static string DSCastScreenCountChangeMessageKey = "DSCastScreenCountChangeMessageKey";

        private double controlCastScreenWidth = 0;
        public double ControlCastScreenWidth { get => controlCastScreenWidth; set => SetProperty(ref controlCastScreenWidth, value); }

        private double controlCastScreenHeight = 0;
        public double ControlCastScreenHeight { get => controlCastScreenHeight; set => SetProperty(ref controlCastScreenHeight, value); }

        private CastScreenItemViewModel controlCastScreen;
        public CastScreenItemViewModel ControlCastScreen { get => controlCastScreen; set => SetProperty(ref controlCastScreen, value); }

        private CastScreenItemViewModel oldControlCastScreen;

        private bool CtrlKeyDown = false;
        private bool isSyncMouse = true;
        private bool isSyncKeyboard = true;
        private bool isSelectAll = false;

        public System.Windows.Controls.ListBox DeviceListBox { get; set; }
        public RelayCommand<int> ListBoxDoubleCommand => new Lazy<RelayCommand<int>>(() => new RelayCommand<int>(ShowControlCastScreen)).Value;
        private ObservableCollection<CastScreenItemViewModel> deviceCollection = new ObservableCollection<CastScreenItemViewModel>();
        public ObservableCollection<CastScreenItemViewModel> DeviceCollection { get => deviceCollection; set => SetProperty(ref deviceCollection, value); }
        private List<CastScreenItemViewModel> DeviceList = new List<CastScreenItemViewModel>();

        public RelayCommand BackCommand => new Lazy<RelayCommand>(() => new RelayCommand(Back)).Value;
        public RelayCommand HomeCommand => new Lazy<RelayCommand>(() => new RelayCommand(Home)).Value;
        public RelayCommand MenuCommand => new Lazy<RelayCommand>(() => new RelayCommand(Menu)).Value;

        public RelayCommand CloseControlCastScreenCommand => new Lazy<RelayCommand>(() => new RelayCommand(CloseControlCastScreen)).Value;

        public DeviceScreenListViewModel()
        {
            CastScreenManageService.Instance.CastScreenManageResult += CastScreenManageResult;
            DeviceService.Instance.DeviceDisconnected += OnDeviceDisconnected;
            DeviceService.Instance.DeviceConnected += OnDeviceConnected;
            DeviceService.Instance.DeviceChange += OnDeviceChanage;

            WeakReferenceMessenger.Default.Register<ValueChangedMessage<double>, string>(this, AllScreenBottomViewModel.ASBVMScreenSizeChanageMessageKey, ScreenSizeChange);

            WeakReferenceMessenger.Default.Register<ValueChangedMessage<int>, string>(this, CastScreenSettingViewModel.CSSCastScreenParamterChangeMessageKey, CastScreenParamterChange);

            WeakReferenceMessenger.Default.Register<ValueChangedMessage<bool>, string>(this, AllScreenTopViewModel.ASTSyncMouseChangeMessageKey, SyncMouse);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<bool>, string>(this, AllScreenTopViewModel.ASTSyncKeyboardChangeMessageKey, SyncKeyboard);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<bool>, string>(this, AllScreenTopViewModel.ASTSelectAllMessageKey, SelectAll);

            WeakReferenceMessenger.Default.Register<ValueChangedMessage<bool>, string>(this, DeviceScreenWindowModel.DSCloseSceenWindowMessageKey, CloseWindow);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<DeviceItemViewModel>, string>(this, AllScreenLeftViewModel.ASLShowCastScreenMessageKey, ShowControlCastScreen);

        }

        public void ViewLoad()
        {
            //读取数组
            List<DeviceModel> deviceList = DeviceService.Instance.DeviceList;
            for (int i = 0; i < deviceList.Count; i++)
            {
                CastScreenManageService.Instance.AddCastScreen(deviceList[i]);
            }
        }

        private void OnDeviceConnected(object sender, EventArg.DeviceEventArgs e)
        {
            if (e.DeviceModel == null) return;
            AddDevice(e.DeviceModel);
        }

        private void OnDeviceDisconnected(object sender, EventArg.DeviceEventArgs e)
        {
            if (e.DeviceModel == null) return;
            CastScreenItemViewModel ob = DeviceList.Find(it =>
            {
                if (it == null) return false;
                return it.Device.Serial.Equals(e.DeviceModel.Serial);
            });
            if (ob != null)
            {
                RemoveDevice(ob);
            }
        }
        private void OnDeviceChanage(object sender, DeviceEventArgs e)
        {
            if (e.DeviceModel == null) return;
            if (e.DeviceModel.State == JXAdbCore.Enums.DeviceState.Offline)
            {
                CastScreenItemViewModel ob = DeviceList.Find(it =>
                {
                    if (it == null) return false;
                    return it.Equals(e.DeviceModel.Serial);
                });
                if (ob != null)
                {
                    RemoveDevice(ob);
                }
            }
        }
        private void AddDevice(DeviceModel device)
        {
            CastScreenManageService.Instance.AddCastScreen(device);
        }
        private void RemoveDevice(CastScreenItemViewModel ob)
        {

            ThreadPool.QueueUserWorkItem(delegate
            {
                SynchronizationContext.SetSynchronizationContext(new
                    DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(pl =>
                {
                    try
                    {
                        if (ob == null) return;
                        ob.CastScreenService.StopReceiveStream();
                        DeviceList.Remove(ob);
                        deviceCollection.Remove(ob);
                        WeakReferenceMessenger.Default.Send<ValueChangedMessage<CastScreenItemViewModel>, string>
                        (new ValueChangedMessage<CastScreenItemViewModel>(ob), DSCastScreenDisconnectMessageKey);
                        WeakReferenceMessenger.Default.Send<ValueChangedMessage<int>, string>
                        (new ValueChangedMessage<int>(DeviceList.Count), DSCastScreenCountChangeMessageKey);
                        if (ob == ControlCastScreen)
                        {
                            CloseControlCastScreen();
                        }
                    }
                    catch (Exception)
                    {
                    }
                }, null);
            });
        }
        private void SyncKeyboard(object recipient, ValueChangedMessage<bool> e) => isSyncKeyboard = e.Value;
        private void SyncMouse(object recipient, ValueChangedMessage<bool> e) => isSyncMouse = e.Value;
        private void SelectAll(object recipient, ValueChangedMessage<bool> e)
        {
            isSelectAll = e.Value;
            foreach (CastScreenItemViewModel ob in DeviceList)
            {
                ob.IsSelect = isSelectAll;
                ob.RefreshItemBorderThickness();
            }
            WeakReferenceMessenger.Default.Send<ValueChangedMessage<bool>, string>
            (new ValueChangedMessage<bool>(isSelectAll), DSCastScreenSelectMessageKey);

        }

        private void CloseWindow(object recipient, ValueChangedMessage<bool> e)
        {
            CastScreenManageService.Instance.CastScreenManageResult -= CastScreenManageResult;
            DeviceService.Instance.DeviceDisconnected -= OnDeviceDisconnected;
            DeviceService.Instance.DeviceConnected -= OnDeviceConnected;
            DeviceService.Instance.DeviceChange -= OnDeviceChanage;

            try
            {
                foreach (CastScreenItemViewModel ob in DeviceList)
                {
                    ob.CastScreenService.StopReceiveStream();
                }
            }
            catch { }
        }

        private void CastScreenManageResult(object sender, CastScreenManageResultEventArgs e)
        {
            if (e.Result)
            {

                ThreadPool.QueueUserWorkItem(delegate
                {
                    SynchronizationContext.SetSynchronizationContext(new
                        DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                    SynchronizationContext.Current.Post(pl =>
                    {
                        try
                        {
                            CastScreenItemViewModel ob = new CastScreenItemViewModel(e.Device, e.CastScreenService);
                            ob.ScreenWidth = AppService.Instance.AppConfig.ScreenCurrentWidth;
                            ob.ScreenHeight = (((ob.CastScreenService.VideoHeight * 1.0) / (ob.CastScreenService.VideoWidth * 1.0)) * ob.ScreenWidth);

                            //int index = DeviceService.Instance.DeviceList.FindIndex(it =>
                            //{
                            //    return it != null && it.Serial.Equals(e.Device.Serial);
                            //});

                            //if (index == -1) index = 0;

                            ////获取排序
                            ///

                            int insertIndex = DeviceList.Count;
                            try
                            {
                                DeviceList.Insert(insertIndex, ob);
                                DeviceCollection.Insert(insertIndex, ob);
                            }
                            catch
                            {
                                DeviceList.Add(ob);
                                DeviceCollection.Add(ob);
                            }

                            Debug.WriteLine($"IP:{e.Device.Serial} 投屏成功DeviceScreenListViewModel");
                            ob.IsSelect = isSelectAll;
                            WeakReferenceMessenger.Default.Send<ValueChangedMessage<CastScreenItemViewModel>, string>(new ValueChangedMessage<CastScreenItemViewModel>(ob), DSCastScreenConnectMessageKey);
                            WeakReferenceMessenger.Default.Send<ValueChangedMessage<int>, string>(new ValueChangedMessage<int>(DeviceList.Count), DSCastScreenCountChangeMessageKey);
                        }
                        catch
                        {
                        }
                        for (int i = 0; i < DeviceList.Count; i++) DeviceList[i].Index = i + 1;
                    }, null);
                });
            }
        }
        private void ScreenSizeChange(object recipient, ValueChangedMessage<double> e)
        {

            for (int i = 0; i < DeviceList.Count; i++)
            {
                CastScreenItemViewModel ob = DeviceList[i];
                ob.ScreenWidth = e.Value;
                ob.ScreenHeight = (((ob.CastScreenService.VideoHeight * 1.0) / (ob.CastScreenService.VideoWidth * 1.0)) * ob.ScreenWidth);
            }
        }
        private void CastScreenParamterChange(object recipient, ValueChangedMessage<int> e)
        {

            for (int i = 0; i < DeviceList.Count; i++)
            {
                CastScreenItemViewModel ob = DeviceList[i];
                if (ControlCastScreen != null && ob.Device.Serial.Equals(ControlCastScreen.Device.Serial)) continue;
                ob.CastScreenService.ResetParamter(AppService.Instance.AppConfig.MiniCastScreenResolution, 1, AppService.Instance.AppConfig.MiniCastScreenRate);
            }
        }

        private void ShowControlCastScreen(object recipient, ValueChangedMessage<DeviceItemViewModel> e)
        {
            int index = -1;
            index = DeviceList.FindIndex(it =>
            {
                return it != null && it.Device.Serial.Equals(e.Value.Device.Serial);
            });
            if (index != -1) ShowControlCastScreen(index);
        }
        private void ShowControlCastScreen(int index)
        {
            try
            {
                if (oldControlCastScreen != null)
                {
                    int miniResolution = AppService.Instance.AppConfig.MiniCastScreenResolution;
                    int miniRate = AppService.Instance.AppConfig.MiniCastScreenRate;
                    oldControlCastScreen.CastScreenService.ResetParamter(miniResolution, 1, miniRate);
                    oldControlCastScreen.IsControl = false;
                }

                int controlResolution = AppService.Instance.AppConfig.ControlCastScreenResolution;
                int controlRate = AppService.Instance.AppConfig.ControlCastScreenRate;

                CastScreenItemViewModel ob = DeviceList[index];
                DeviceModel device = ob.Device;
                double width = (int)DeviceListBox.ActualWidth;
                ControlCastScreenWidth = 450;
                ControlCastScreenHeight = ((device.ScreenSize.Height * 1.0) / (device.ScreenSize.Width * 1.0)) * ControlCastScreenWidth + 70;
                ControlCastScreen = DeviceList[index];
                ControlCastScreen.CastScreenService.ResetParamter(controlResolution, 1, controlRate);
                ob.IsControl = true;
                oldControlCastScreen = ControlCastScreen;
            }
            catch { }
        }
        private void Back()
        {
            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Mouse), new KeycodeControlMessage { KeyCode = AndroidKeycode.AKEYCODE_BACK, Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN });
            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Mouse), new KeycodeControlMessage { KeyCode = AndroidKeycode.AKEYCODE_BACK, Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP });
        }
        private void Home()
        {
            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Mouse), new KeycodeControlMessage { KeyCode = AndroidKeycode.AKEYCODE_HOME, Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN });
            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Mouse), new KeycodeControlMessage { KeyCode = AndroidKeycode.AKEYCODE_HOME, Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP });
        }
        private void Menu()
        {
            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Mouse), new KeycodeControlMessage { KeyCode = AndroidKeycode.AKEYCODE_APP_SWITCH, Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN });
            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Mouse), new KeycodeControlMessage { KeyCode = AndroidKeycode.AKEYCODE_APP_SWITCH, Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP });
        }
        private List<CastScreenItemViewModel> GetNeedSyncDeviceList(SyncType type)
        {
            List<CastScreenItemViewModel> list = DeviceList.FindAll(it =>
            {
                return ((it.IsSelect == true && (type == SyncType.Mouse ? isSyncMouse : (type == SyncType.Keyboard ? isSyncKeyboard : false))) ||
                (it.Device.Serial.Equals(ControlCastScreen.Device.Serial)));
            });
            return list;
        }
        private void CloseControlCastScreen()
        {

            int miniResolution = AppService.Instance.AppConfig.MiniCastScreenResolution;
            int miniRate = AppService.Instance.AppConfig.MiniCastScreenRate;
            ControlCastScreen.CastScreenService.ResetParamter(miniResolution, 1, miniRate);
            ControlCastScreenWidth = 0;
            ControlCastScreenHeight = 0;
            ControlCastScreen.IsControl = false;
        }

        #region 屏幕鼠标操作
        public void PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Mouse), GetNeedAsyncIControlMessage((Image)sender, e, SyncType.Mouse, AndroidMotionEventAction.AMOTION_EVENT_ACTION_DOWN));
        }
        public void PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Mouse), GetNeedAsyncIControlMessage((Image)sender, e, SyncType.Mouse, AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP));

        }
        public void PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Mouse), GetNeedAsyncIControlMessage((Image)sender, e, SyncType.Mouse, AndroidMotionEventAction.AMOTION_EVENT_ACTION_MOVE));

        }
        public void PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {

            //取消ctrl 按下状态
            if (e.Key == Key.LeftCtrl || e.Key == Key.Right) CtrlKeyDown = false;

            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Keyboard), GetNeedAsyncIControlMessage(null, e, SyncType.Keyboard, AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, AndroidKeyEventAction.AKEY_EVENT_ACTION_UP));

        }

        public void PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

            //取消ctrl 按下状态
            if (e.Key == Key.LeftCtrl || e.Key == Key.Right) CtrlKeyDown = true;

            //Ctrl + V 执行黏贴操作 
            if (e.Key == Key.V && CtrlKeyDown)
            {
                Thread sendTextThread = new Thread(() =>
                {
                    try
                    {
                        string text = System.Windows.Clipboard.GetText();
                        List<CastScreenItemViewModel> list = GetNeedSyncDeviceList(SyncType.Keyboard);
                        foreach (CastScreenItemViewModel ob in list)
                        {
                            AdbService.Instance.SendText(ob.Device, text);
                        }
                    }
                    catch
                    {
                    }
                });
                sendTextThread.IsBackground = true;
                sendTextThread.SetApartmentState(ApartmentState.STA);
                sendTextThread.Start();
            }
            else
            {
                SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Keyboard), GetNeedAsyncIControlMessage(null, e, SyncType.Keyboard, AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN));
            }

        }

        public void PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            SendMessageToDevice(GetNeedSyncDeviceList(SyncType.Mouse), GetNeedAsyncIControlMessage((Image)sender, e));
        }


        private void SendMessageToDevice(List<CastScreenItemViewModel> obList, List<IControlMessage> iControlMessageList)
        {

            new Thread(() =>
            {
                for (int i = 0; i < obList.Count; i++)
                {
                    try
                    {
                        CastScreenItemViewModel ob = obList[i];
                        IControlMessage cmd = iControlMessageList[i]; ;
                        ob.CastScreenService.SendMessage(cmd.bytes(), 0, cmd.size);
                    }
                    catch (Exception ex)
                    {
                    }
                }

            }).Start();
        }

        private void SendMessageToDevice(List<CastScreenItemViewModel> obList, IControlMessage iControlMessage)
        {

            for (int i = 0; i < obList.Count; i++)
            {
                CastScreenItemViewModel ob = obList[i];
                try
                {
                    ob.CastScreenService.SendMessage(iControlMessage.bytes(), 0, iControlMessage.size);
                }
                catch
                {
                }
            }
        }

        private List<IControlMessage> GetNeedAsyncIControlMessage(Image sender, InputEventArgs e, SyncType type = SyncType.Mouse, AndroidMotionEventAction action = AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, AndroidKeyEventAction keyAction = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP)
        {

            List<CastScreenItemViewModel> obList = GetNeedSyncDeviceList(type);
            List<IControlMessage> iControlMessageList = new List<IControlMessage>();


            foreach (CastScreenItemViewModel ob in obList)
            {


                IControlMessage controlMessage = null;

                if (e is MouseButtonEventArgs)
                {
                    controlMessage = new TouchEventControlMessage()
                    {
                        Position = GetMousePosition(sender, e as MouseButtonEventArgs, ob),
                        Action = action
                    };
                }
                else if (e is MouseWheelEventArgs)
                {
                    int delta = ((MouseWheelEventArgs)e).Delta;
                    var msg = new ScrollEventControlMessage();
                    msg.Position = GetMousePosition(sender, e as MouseWheelEventArgs, ob);
                    msg.VerticalScroll = delta > 0 ? 2 : -2;
                    msg.HorizontalScroll = delta;
                    controlMessage = msg;
                }
                else if (e is System.Windows.Input.MouseEventArgs)
                {

                    controlMessage = new TouchEventControlMessage()
                    {
                        Position = GetMousePosition(sender, e as System.Windows.Input.MouseEventArgs, ob),
                        Action = action
                    };
                }
                else if (e is System.Windows.Input.KeyEventArgs)
                {
                    KeycodeControlMessage msg = new KeycodeControlMessage();
                    if (keyAction == AndroidKeyEventAction.AKEY_EVENT_ACTION_UP) msg.Action = keyAction;
                    msg.KeyCode = KeycodeHelper.ConvertKey((e as System.Windows.Input.KeyEventArgs).Key);
                    msg.Metastate = KeycodeHelper.ConvertModifiers((e as System.Windows.Input.KeyEventArgs).KeyboardDevice.Modifiers);
                    controlMessage = msg;
                }
                else
                {
                }
                iControlMessageList.Add(controlMessage);
            }
            return iControlMessageList;
        }

        private Position GetMousePosition(Image sender, System.Windows.Input.MouseEventArgs e, CastScreenItemViewModel ob)
        {

            try
            {
                var point = e.GetPosition((IInputElement)sender);

                var pos = new Position();
                pos.Point = new System.Drawing.Point((int)point.X, (int)point.Y);
                pos.ScreenSize.Width = (ushort)sender.ActualWidth;
                pos.ScreenSize.Height = (ushort)sender.ActualHeight;

                double scale = (double)ob.CastScreenService.VideoWidth / pos.ScreenSize.Width;

                pos.ScreenSize.Width = (ushort)ob.CastScreenService.VideoWidth;
                pos.ScreenSize.Height = (ushort)ob.CastScreenService.VideoHeight;
                pos.Point.X = (ushort)(scale * pos.Point.X);
                pos.Point.Y = (ushort)(scale * pos.Point.Y);

                return pos;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

