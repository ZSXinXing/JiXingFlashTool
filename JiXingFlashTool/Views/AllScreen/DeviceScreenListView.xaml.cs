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
using System.Windows.Navigation;
using System.Windows.Shapes;
using JiXingFlashTool.ViewModels;
using JiXingFlashTool.ViewModels.AllScreen;

namespace JiXingFlashTool.Views.AllScreen
{
    /// <summary>
    /// DeviceScreenListView.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceScreenListView : UserControl
    {
        public DeviceScreenListView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceScreenListViewModel vm = (DeviceScreenListViewModel)DataContext;
            vm.DeviceListBox = DeviceListBox;
            vm.ViewLoad();
        }

        private void ControlD3dIS_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(sender as Image);
            DeviceScreenListViewModel viewModel = (DeviceScreenListViewModel)DataContext;
            viewModel.PreviewMouseLeftButtonDown(sender, e);
        }

        private void ControlD3dIS_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Keyboard.Focus(sender as Image);
            DeviceScreenListViewModel viewModel = (DeviceScreenListViewModel)DataContext;
            viewModel.PreviewMouseMove(sender, e);
        }

        private void ControlD3dIS_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(sender as Image);
            DeviceScreenListViewModel viewModel = (DeviceScreenListViewModel)DataContext;
            viewModel.PreviewMouseLeftButtonUp(sender, e);
        }

        private void ControlD3dIS_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void ControlD3dIS_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(sender as Image);
        }

        private void ControlD3dIS_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Keyboard.Focus(sender as Image);
            DeviceScreenListViewModel viewModel = (DeviceScreenListViewModel)DataContext;
            viewModel.PreviewMouseWheel(sender, e);
        }

        private void ControlD3dIS_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            DeviceScreenListViewModel viewModel = (DeviceScreenListViewModel)DataContext;
            viewModel.PreviewKeyDown(sender, e);
        }

        private void ControlD3dIS_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            DeviceScreenListViewModel viewModel = (DeviceScreenListViewModel)DataContext;
            viewModel.PreviewKeyUp(sender, e);
        }
    }
}
