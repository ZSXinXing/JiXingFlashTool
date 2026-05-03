using JiXingFlashTool.ViewModels;
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

namespace JiXingFlashTool.Views
{
    /// <summary>
    /// DeviceScreenView.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceScreenView : Window
    {
        public DeviceScreenView()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceScreenViewModel vm = (DeviceScreenViewModel)DataContext;
            vm.DeviceScreenView = this;

            //  this.MouseDown += delegate { DragMove(); };
        }

        public void ReSetSize(double width, double height)
        {
        }

        private void DockPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void d3dIS_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Focus();
            DeviceScreenViewModel viewModel = (DeviceScreenViewModel)DataContext;
            viewModel.PreviewMouseLeftButtonDown(sender, e);
        }

        private void d3dIS_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            this.Focus();
            DeviceScreenViewModel viewModel = (DeviceScreenViewModel)DataContext;
            viewModel.PreviewMouseMove(sender, e);
        }

        private void d3dIS_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Focus();
            DeviceScreenViewModel viewModel = (DeviceScreenViewModel)DataContext;
            viewModel.PreviewMouseLeftButtonUp(sender, e);
        }

        private void d3dIS_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void d3dIS_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void d3dIS_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.Focus();
            DeviceScreenViewModel viewModel = (DeviceScreenViewModel)DataContext;
            viewModel.PreviewMouseWheel(sender, e);
        }

        private void d3dIS_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            DeviceScreenViewModel viewModel = (DeviceScreenViewModel)DataContext;
            viewModel.PreviewKeyDown(sender, e);
        }

        private void d3dIS_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            DeviceScreenViewModel viewModel = (DeviceScreenViewModel)DataContext;
            viewModel.PreviewKeyUp(sender, e);
        }
    }
}
