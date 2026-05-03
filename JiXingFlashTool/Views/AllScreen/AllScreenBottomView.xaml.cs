using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using JiXingFlashTool.ViewModels.AllScreen;

namespace JiXingFlashTool.Views.AllScreen
{
    /// <summary>
    /// AllScreenBottomView.xaml 的交互逻辑
    /// </summary>
    public partial class AllScreenBottomView : UserControl
    {
        public AllScreenBottomView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AllScreenBottomViewModel vm = (AllScreenBottomViewModel)DataContext;
            vm.ViewLoad();
        }

        /// <summary>
        /// 画面大小滑块变化时，将当前值同步给底部视图模型。
        /// </summary>
        /// <param name="sender">触发事件的滑块。</param>
        /// <param name="e">滑块值变化事件参数。</param>
        private void PreviewSliderHorizontal_PreviewPositionChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AllScreenBottomViewModel vm = (AllScreenBottomViewModel)DataContext;
            double value = e.NewValue;
            vm.ValueChange(value);
        }
    }
}
