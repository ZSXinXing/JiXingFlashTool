using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JiXingFlashTool.Model;
using JiXingFlashTool.Services;
using JiXingFlashTool.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using JiXingFlashTool.ObservableModel;
using JiXingFlashTool.Enums;
using JiXingFlashTool.EventArg;
using JiXingFlashTool.Utils;
using System.Windows.Threading;
using System.Windows.Controls;

namespace JiXingFlashTool.ViewModels
{
    public class DeviceScreenViewModel : ObservableObject
    {
        private static int MAX_WINDOW_HEIGHT = 750;
        private static int MAX_WINDOW_WIDTH = 800;

        private bool CtrlKeyDown = false;

        private double screenWidth;
        public double ScreenWidth { get => screenWidth; set => SetProperty(ref screenWidth, value); }

        private double screenHeight;
        public double ScreenHeight { get => screenHeight; set => SetProperty(ref screenHeight, value); }


        private string title;
        public string Title { get => title; set => SetProperty(ref title, value); }

        private ObservableCastScreenModel ob;
        public ObservableCastScreenModel OB { get => ob; set => SetProperty(ref ob, value); }

        public RelayCommand CloseCommand => new Lazy<RelayCommand>(() => new RelayCommand(CloseWindow)).Value;
        public RelayCommand MiniCommand => new Lazy<RelayCommand>(() => new RelayCommand(MiniWindow)).Value;
        public RelayCommand BackCommand => new Lazy<RelayCommand>(() => new RelayCommand(Back)).Value;
        public RelayCommand HomeCommand => new Lazy<RelayCommand>(() => new RelayCommand(Home)).Value;
        public RelayCommand MenuCommand => new Lazy<RelayCommand>(() => new RelayCommand(Menu)).Value;

        public DeviceScreenView DeviceScreenView { get; set; }
        private DeviceModel Device { get; set; }
        private Thread StartScreenThread { get; set; }
        #region 公共方法
        public static DeviceScreenViewModel Show(DeviceModel device)
        {
            DeviceScreenViewModel viewModel = new DeviceScreenViewModel();
            DeviceScreenView view = new DeviceScreenView();
            viewModel.Title = $"{device.Serial}-{device.Name}";
            view.DataContext = viewModel;
            view.Show();
            viewModel.Device = device;
            return viewModel;
        }
        #endregion

        public void StartScreen(int resolution, int rate)
        {
            CastScreenService service = new CastScreenService(Device);
            service.ScreenResult += OnScreenResult;
            service.D3DChanage += OnD3DChanage;
            service.Start(resolution, 1, rate);
        }

        private void DeviceDisconnected(object sender, EventArg.DeviceEventArgs e)
        {
            CloseWindow();
        }

        private void OnD3DChanage(object sender, EventArg.ScreenD3DEventArgs e)
        {

            ThreadPool.QueueUserWorkItem(delegate
            {
                SynchronizationContext.SetSynchronizationContext(new
                    DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(pl =>
                {
                    double height = 0, width = 0;

                    //竖屏
                    if (e.Width <= e.Height)
                    {
                        height = MAX_WINDOW_HEIGHT - 30 - 40;
                        width = height * ((e.Width * 1.0) / (e.Height * 1.0));
                        DeviceScreenView.Width = width;
                        DeviceScreenView.Height = MAX_WINDOW_HEIGHT;
                    }
                    else
                    {//横屏 
                        width = MAX_WINDOW_WIDTH;
                        height = width * ((e.Height * 1.0) / (e.Width * 1.0));
                        DeviceScreenView.Width = MAX_WINDOW_WIDTH;
                        DeviceScreenView.Height = height + 32 + 40;
                    }

                    //ScreenWidth = width;
                    //ScreenHeight = height;
                    DeviceScreenView.d3dIS.Width = width;
                    DeviceScreenView.d3dIS.Height = height;
                }, null);
            });

        }

        private void CloseWindow()
        {
            if (OB != null)
            {
                if (ob.Service != null)
                    ob.CastScreenService.StopReceiveStream();
            }
            DeviceScreenView.Close();

        }

        private void MiniWindow()
        {
            DeviceScreenView.WindowState = System.Windows.WindowState.Minimized;
        }

        private void Back()
        {
            AdbService.Instance.Back(Device);
        }

        private void Home()
        {
            AdbService.Instance.Home(Device);
        }

        private void Menu()
        {
            AdbService.Instance.Menu(Device);
        }


        private void Close()
        {

        }

        #region 屏幕鼠标操作
        public void PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SendMessageToDevice(GetNeedAsyncIControlMessage((Image)sender, e, AndroidMotionEventAction.AMOTION_EVENT_ACTION_DOWN));
        }
        public void PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            SendMessageToDevice(GetNeedAsyncIControlMessage((Image)sender, e, AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP));

        }
        public void PreviewMouseMove(object sender, MouseEventArgs e)
        {
            SendMessageToDevice(GetNeedAsyncIControlMessage((Image)sender, e, AndroidMotionEventAction.AMOTION_EVENT_ACTION_MOVE));

        }
        public void PreviewKeyUp(object sender, KeyEventArgs e)
        {

            //取消ctrl 按下状态
            if (e.Key == Key.LeftCtrl || e.Key == Key.Right) CtrlKeyDown = false;

            SendMessageToDevice(GetNeedAsyncIControlMessage(null, e, AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, AndroidKeyEventAction.AKEY_EVENT_ACTION_UP));

        }


        public void PreviewKeyDown(object sender, KeyEventArgs e)
        {

            //取消ctrl 按下状态
            if (e.Key == Key.LeftCtrl || e.Key == Key.Right) CtrlKeyDown = true;

            //Ctrl + V 执行黏贴操作 
            if (e.Key == Key.V && CtrlKeyDown)
            {
                AdbService.Instance.SendText(Device, Clipboard.GetText());
            }
            else
            {
                SendMessageToDevice(GetNeedAsyncIControlMessage(null, e, AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN));
            }

        }

        /// <summary>
        /// 鼠标滚动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            SendMessageToDevice(GetNeedAsyncIControlMessage((Image)sender, e));
        }
        #endregion

        private void SendMessageToDevice(IControlMessage cmd)
        {
            try
            {
                OB.CastScreenService.SendMessage(cmd.bytes(), 0, cmd.size);
            }
            catch
            {
            }
        }

        private IControlMessage GetNeedAsyncIControlMessage(Image sender, InputEventArgs e, AndroidMotionEventAction action = AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, AndroidKeyEventAction keyAction = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP)
        {
            IControlMessage controlMessage = null;

            if (e is MouseButtonEventArgs)
            {
                controlMessage = new TouchEventControlMessage()
                {
                    Position = GetMousePosition(sender, e as MouseButtonEventArgs, Device),
                    Action = action
                };
            }
            else if (e is MouseWheelEventArgs)
            {
                int delta = ((MouseWheelEventArgs)e).Delta;
                var msg = new ScrollEventControlMessage();
                msg.Position = GetMousePosition(sender, e as MouseWheelEventArgs, Device);
                msg.VerticalScroll = delta > 0 ? 2 : -2;
                msg.HorizontalScroll = delta;
                controlMessage = msg;
            }
            else if (e is MouseEventArgs)
            {

                controlMessage = new TouchEventControlMessage()
                {
                    Position = GetMousePosition(sender, e as MouseEventArgs, Device),
                    Action = action
                };
            }
            else if (e is KeyEventArgs)
            {
                KeycodeControlMessage msg = new KeycodeControlMessage();
                if (keyAction == AndroidKeyEventAction.AKEY_EVENT_ACTION_UP) msg.Action = keyAction;
                msg.KeyCode = KeycodeHelper.ConvertKey((e as KeyEventArgs).Key);
                msg.Metastate = KeycodeHelper.ConvertModifiers((e as KeyEventArgs).KeyboardDevice.Modifiers);
                controlMessage = msg;
            }
            else
            {
            }
            return controlMessage;
        }

        private Position GetMousePosition(Image sender, MouseEventArgs e, DeviceModel device)
        {

            try
            {
                var point = e.GetPosition((IInputElement)sender);

                var pos = new Position();
                pos.Point = new System.Drawing.Point((int)point.X, (int)point.Y);
                pos.ScreenSize.Width = (ushort)sender.ActualWidth;
                pos.ScreenSize.Height = (ushort)sender.ActualHeight;

                double scale = (double)OB.CastScreenService.VideoWidth / pos.ScreenSize.Width;

                pos.ScreenSize.Width = (ushort)OB.CastScreenService.VideoWidth;
                pos.ScreenSize.Height = (ushort)OB.CastScreenService.VideoHeight;
                pos.Point.X = (ushort)(scale * pos.Point.X);
                pos.Point.Y = (ushort)(scale * pos.Point.Y);

                return pos;
            }
            catch
            {
                return null;
            }
        }

        private void OnScreenResult(object sender, CastScreenResultEventArgs e)
        {

            CastScreenService screenService = (CastScreenService)sender;
            if (e.Result)
            {
                ObservableCastScreenModel ob = new ObservableCastScreenModel(e.Device, screenService);
                this.OB = ob;

                Debug.WriteLine("投屏成功");
            }
            else
            {
                //投屏失败就继续任务
                CastScreenService service = (CastScreenService)sender;
                service.Start(720, 1, 30);
                Debug.WriteLine("投屏失败");
            }
        }
    }
}
