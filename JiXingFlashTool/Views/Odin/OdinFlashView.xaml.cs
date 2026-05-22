using JiXingFlashTool.ViewModels.Odin;
using System.Windows.Controls;

namespace JiXingFlashTool.Views.Odin
{
    /// <summary>
    /// Odin 刷机页面视图，仅负责界面展示和绑定承载。
    /// </summary>
    public partial class OdinFlashView : UserControl
    {
        /// <summary>
        /// 初始化 Odin 刷机页面视图。
        /// </summary>
        public OdinFlashView()
        {
            InitializeComponent();
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// 页面卸载时释放 ViewModel 中的设备监听资源。
        /// </summary>
        /// <param name="sender">事件来源。</param>
        /// <param name="e">事件参数。</param>
        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is OdinFlashViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}
