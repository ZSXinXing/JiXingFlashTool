using JiXingFlashTool.ViewModels.AllScreen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JiXingFlashTool.Views.AllScreen
{
    /// <summary>
    /// DeviceScreenWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceScreenWindow : Window
    {
        public DeviceScreenWindow()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            DeviceScreenWindowModel vm = (DeviceScreenWindowModel)DataContext;
            vm.CloseWindow();
        }
    }
}
