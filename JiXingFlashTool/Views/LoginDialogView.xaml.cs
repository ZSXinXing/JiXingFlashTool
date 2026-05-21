using System.Windows.Controls;
using JiXingFlashTool.ViewModels;

namespace JiXingFlashTool.Views
{
    /// <summary>
    /// 专业模式登录弹窗视图。
    /// </summary>
    public partial class LoginDialogView : UserControl
    {
        /// <summary>
        /// 初始化专业模式登录弹窗视图。
        /// </summary>
        public LoginDialogView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 将密码框内容同步到视图模型。
        /// </summary>
        /// <param name="sender">密码框对象。</param>
        /// <param name="e">事件参数。</param>
        private void HiddenPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginDialogViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
            }
        }

        /// <summary>
        /// 密码框重新显示时回填当前密码，避免显隐切换后内容丢失。
        /// </summary>
        /// <param name="sender">密码框对象。</param>
        /// <param name="e">事件参数。</param>
        private void HiddenPasswordBox_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox passwordBox &&
                DataContext is LoginDialogViewModel viewModel &&
                passwordBox.IsVisible &&
                passwordBox.Password != viewModel.Password)
            {
                passwordBox.Password = viewModel.Password ?? string.Empty;
            }
        }
    }
}
