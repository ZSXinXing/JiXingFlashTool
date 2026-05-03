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
    /// AllScreenLeftView.xaml 的交互逻辑
    /// </summary>
    public partial class AllScreenLeftView : UserControl
    {
        public AllScreenLeftView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AllScreenLeftViewModel vm = (AllScreenLeftViewModel)DataContext;
            vm.ViewLoad();
        }
    }
}
