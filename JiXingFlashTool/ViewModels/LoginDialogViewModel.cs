using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using LanguageCore;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace JiXingFlashTool.ViewModels
{
    /// <summary>
    /// 维护者登录弹窗视图模型，负责登录表单状态、异步登录流程与错误提示。
    /// </summary>
    public class LoginDialogViewModel : ObservableObject
    {
        private const string DefaultUserName = "admin";
        private const string DefaultPassword = "admin";
        private readonly Action _loginSucceededAction;
        private readonly RelayCommand _cancelCommand;
        private readonly RelayCommand _togglePasswordVisibilityCommand;
        private readonly AsyncRelayCommand _loginCommand;
        private string _userName = string.Empty;
        private string _password = string.Empty;
        private bool _isPasswordVisible;
        private bool _isLoggingIn;
        private string _errorMessage = string.Empty;

        /// <summary>
        /// 初始化维护者登录弹窗视图模型。
        /// </summary>
        /// <param name="loginSucceededAction">登录成功后的回调。</param>
        public LoginDialogViewModel(Action loginSucceededAction)
        {
            _loginSucceededAction = loginSucceededAction ?? throw new ArgumentNullException(nameof(loginSucceededAction));
            _cancelCommand = new RelayCommand(Cancel);
            _togglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);
            _loginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
            WeakEventManager<LocalizationService, EventArgs>.AddHandler(
                LocalizationService.Instance,
                nameof(LocalizationService.LanguageChanged),
                OnLanguageChanged);
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
        /// 当前是否显示明文密码。
        /// </summary>
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }

        /// <summary>
        /// 当前是否正在执行登录请求。
        /// </summary>
        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set
            {
                if (SetProperty(ref _isLoggingIn, value))
                {
                    OnPropertyChanged(nameof(LoginButtonText));
                    _loginCommand.NotifyCanExecuteChanged();
                }
            }
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
        /// 登录按钮当前文案。
        /// </summary>
        public string LoginButtonText => IsLoggingIn ? GetLangText("Login_ButtonLoading") : GetLangText("Login_Button");

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
        public AsyncRelayCommand LoginCommand => _loginCommand;

        /// <summary>
        /// 异步执行维护者登录。
        /// </summary>
        private async Task LoginAsync()
        {
            IsLoggingIn = true;

            try
            {
                // 预留真实网络请求位置，当前用短暂异步等待模拟登录过程。
                await Task.Delay(600);

                if (!string.Equals(UserName?.Trim(), DefaultUserName, StringComparison.Ordinal) ||
                    !string.Equals(Password, DefaultPassword, StringComparison.Ordinal))
                {
                    ErrorMessage = GetLangText("Login_InvalidCredential");
                    OnPropertyChanged(nameof(HasErrorMessage));
                    return;
                }

                _loginSucceededAction.Invoke();
                Dialog?.Close();
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        /// <summary>
        /// 判断当前是否允许发起登录。
        /// </summary>
        private bool CanLogin()
        {
            return !IsLoggingIn;
        }

        /// <summary>
        /// 关闭当前弹窗。
        /// </summary>
        private void Cancel()
        {
            if (IsLoggingIn)
            {
                return;
            }

            Dialog?.Close();
        }

        /// <summary>
        /// 切换密码是否显示明文。
        /// </summary>
        private void TogglePasswordVisibility()
        {
            if (IsLoggingIn)
            {
                return;
            }

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

        /// <summary>
        /// 获取当前语言下的登录弹窗文案。
        /// </summary>
        /// <param name="key">语言资源键。</param>
        /// <returns>当前语言对应文案。</returns>
        private static string GetLangText(string key)
        {
            return LocalizationService.Instance.GetString(string.Empty, key);
        }

        /// <summary>
        /// 语言切换后刷新登录按钮动态文案。
        /// </summary>
        /// <param name="sender">事件来源。</param>
        /// <param name="e">事件参数。</param>
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(LoginButtonText));
        }
    }
}
