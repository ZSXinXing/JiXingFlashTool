using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using JiXingFlashTool.Views.AllScreen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using JiXingFlashTool.ItemViewModel;

namespace JiXingFlashTool.ViewModels.AllScreen
{
    public partial class DeviceScreenWindowModel : ObservableObject
    {

        public static string DSCloseSceenWindowMessageKey = "DSCloseSceenWindowMessageKey";

        /// <summary>
        /// 当前窗口宽度。
        /// </summary>
        private double screenWidth;

        /// <summary>
        /// 当前窗口宽度。
        /// </summary>
        public double ScreenWidth
        {
            get => screenWidth;
            set => SetProperty(ref screenWidth, value);
        }

        /// <summary>
        /// 当前窗口高度。
        /// </summary>
        private double screenHeight;

        /// <summary>
        /// 当前窗口高度。
        /// </summary>
        public double ScreenHeight
        {
            get => screenHeight;
            set => SetProperty(ref screenHeight, value);
        }

        /// <summary>
        /// 窗口标题。
        /// </summary>
        private string title;

        /// <summary>
        /// 窗口标题。
        /// </summary>
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        /// <summary>
        /// 当前窗口对应的投屏项。
        /// </summary>
        private CastScreenItemViewModel ob;

        /// <summary>
        /// 当前窗口对应的投屏项。
        /// </summary>
        public CastScreenItemViewModel OB
        {
            get => ob;
            set => SetProperty(ref ob, value);
        }

        /// <summary>
        /// 关闭窗口命令。
        /// </summary>
        public RelayCommand CloseCommand => new Lazy<RelayCommand>(() => new RelayCommand(Close)).Value;

        /// <summary>
        /// 关闭窗口。
        /// </summary>
        private void Close()
        {
            CloseWindow();
        }

        public static DeviceScreenWindowModel Show()
        {
            DeviceScreenWindowModel viewModel = new DeviceScreenWindowModel();
            DeviceScreenWindow window = new DeviceScreenWindow();
            window.DataContext = viewModel;
            window.Show();
            return viewModel;
        }

        public void CloseWindow()
        {
            WeakReferenceMessenger.Default.Send<ValueChangedMessage<bool>, string>(new ValueChangedMessage<bool>(true), DSCloseSceenWindowMessageKey);
        }

    }
}
