using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using JiXingFlashTool.Views.AllScreen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.ViewModels.AllScreen
{
    public class DeviceScreenWindowModel : ObservableObject
    {

        public static string DSCloseSceenWindowMessageKey = "DSCloseSceenWindowMessageKey";
        public DeviceScreenWindowModel()
        {
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
