using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using System;

namespace JiXingFlashTool.ViewModels
{
    /// <summary>
    /// 专业模式登录弹窗视图模型，负责登录表单状态、校验提示与弹窗关闭流程。
    /// </summary>
    public class LoginDialogViewModel : ObservableObject
    {
        private const string DefaultUserName = "admin";
        private const string DefaultPassword = "admin";
        private readonly Action _loginSucceededAction;
        private readonly RelayCommand _cancelCommand;
        private readonly RelayCommand _togglePasswordVisibilityCommand;
        private readonly RelayCommand _loginCommand;
        private string _userName = string.Empty;
        private string _password = string.Empty;
        private bool _isPasswordVisible;
        private string _errorMessage = string.Empty;

        /// <summary>
        /// 初始化专业模式登录弹窗视图模型。
        /// </summary>
        /// <param name="loginSucceededAction">登录成功后的回调。</param>
        public LoginDialogViewModel(Action loginSucceededAction)
        {
            _loginSucceededAction = loginSucceededAction ?? throw new ArgumentNullException(nameof(loginSucceededAction));
            _cancelCommand = new RelayCommand(Cancel);
            _togglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);
            _loginCommand = new RelayCommand(Login);
        }

        /// <summary>
        /// 当前 HandyControl 弹窗实例。
        /// </summary>
        public Dialog Dialog { get; set; }

        /// <summary>
        /// 当前输入的用户名。
        /// </summary>
        public string UserName
        {
            get => _userName;
            set
            {
                if (SetProperty(ref _userName, value))
                {
                    ClearErrorMessage();
                }
            }
        }

        /// <summary>
        /// 当前输入的密码。
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ClearErrorMessage();
                }
            }
        }

        /// <summary>
        /// 当前是否以明文显示密码。
        /// </summary>
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }

        /// <summary>
        /// 当前错误提示文案。
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// 当前是否存在错误提示。
        /// </summary>
        public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// 取消并关闭弹窗命令。
        /// </summary>
        public RelayCommand CancelCommand => _cancelCommand;

        /// <summary>
        /// 切换密码显示方式命令。
        /// </summary>
        public RelayCommand TogglePasswordVisibilityCommand => _togglePasswordVisibilityCommand;

        /// <summary>
        /// 登录命令。
        /// </summary>
        public RelayCommand LoginCommand => _loginCommand;

        /// <summary>
        /// 执行维护者登录。
        /// </summary>
        private void Login()
        {
            if (!string.Equals(UserName?.Trim(), DefaultUserName, StringComparison.Ordinal) ||
                !string.Equals(Password, DefaultPassword, StringComparison.Ordinal))
            {
                ErrorMessage = "\u7528\u6237\u540D\u6216\u5BC6\u7801\u9519\u8BEF\uFF0C\u8BF7\u91CD\u65B0\u8F93\u5165\u3002";
                OnPropertyChanged(nameof(HasErrorMessage));
                return;
            }

            _loginSucceededAction.Invoke();
            Dialog?.Close();
        }

        /// <summary>
        /// 关闭当前弹窗。
        /// </summary>
        private void Cancel()
        {
            Dialog?.Close();
        }

        /// <summary>
        /// 切换密码是否显示明文。
        /// </summary>
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        /// <summary>
        /// 清理错误提示并通知界面刷新。
        /// </summary>
        private void ClearErrorMessage()
        {
            if (string.IsNullOrWhiteSpace(ErrorMessage))
            {
                return;
            }

            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasErrorMessage));
        }
    }
}
