using JiXingFlashTool.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace JiXingFlashTool.Views
{
    /// <summary>
    /// 主窗口视图，仅负责加载、关闭和标题栏外观同步。
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int WmSetIcon = 0x0080;
        private const int GwlExStyle = -20;
        private const int WsExDlgModalFrame = 0x0001;
        private const int DwmwaBorderColor = 34;
        private const int DwmwaCaptionColor = 35;
        private const int DwmwaTextColor = 36;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpFrameChanged = 0x0020;
        private static readonly Color FixedCaptionBackgroundColor = Color.FromRgb(255, 255, 255);
        private static readonly Color FixedCaptionTextColor = Color.FromRgb(31, 35, 40);
        private static readonly Color FixedCaptionBorderColor = Color.FromRgb(229, 231, 235);

        /// <summary>
        /// 初始化主窗口。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += Window_SourceInitialized;
            Closing += Window_Closing;
        }

        /// <summary>
        /// 窗口加载后初始化主页面数据。
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = (MainWindowViewModel)DataContext;
            viewModel.ViewLoad();
        }

        private void ProfessionalDropDownPopup_Closed(object sender, EventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.Popup popup &&
                popup.PlacementTarget is System.Windows.Controls.Primitives.ToggleButton toggleButton)
            {
                toggleButton.IsChecked = false;
            }
        }

        private void ProfessionalDropDownButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == ProfessionalRebootDropDownButton)
            {
                ProfessionalClearDropDownButton.IsChecked = false;
                ProfessionalUpdateDropDownButton.IsChecked = false;
            }
            else if (sender == ProfessionalClearDropDownButton)
            {
                ProfessionalRebootDropDownButton.IsChecked = false;
                ProfessionalUpdateDropDownButton.IsChecked = false;
            }
            else if (sender == ProfessionalUpdateDropDownButton)
            {
                ProfessionalRebootDropDownButton.IsChecked = false;
                ProfessionalClearDropDownButton.IsChecked = false;
            }
        }

        /// <summary>
        /// 窗口源初始化后清空标题栏图标并固定标题栏颜色。
        /// </summary>
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            RemoveWindowCaptionIcon();
            ApplyFixedWindowCaptionColors();
        }

        /// <summary>
        /// 窗口关闭时退出应用。
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
            Environment.Exit(0);
        }

        /// <summary>
        /// 清空系统标题栏左上角图标，避免继承默认应用图标。
        /// </summary>
        private void RemoveWindowCaptionIcon()
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            SendMessage(windowHandle, WmSetIcon, IntPtr.Zero, IntPtr.Zero);
            SendMessage(windowHandle, WmSetIcon, new IntPtr(1), IntPtr.Zero);

            var currentExStyle = GetWindowLongPtr(windowHandle, GwlExStyle);
            var updatedExStyle = new IntPtr(currentExStyle.ToInt64() | WsExDlgModalFrame);
            if (updatedExStyle != currentExStyle)
            {
                SetWindowLongPtr(windowHandle, GwlExStyle, updatedExStyle);
            }

            SetWindowPos(
                windowHandle,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
        }

        /// <summary>
        /// 固定系统标题栏颜色。
        /// </summary>
        private void ApplyFixedWindowCaptionColors()
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            TrySetDwmWindowColorAttribute(windowHandle, DwmwaCaptionColor, FixedCaptionBackgroundColor);
            TrySetDwmWindowColorAttribute(windowHandle, DwmwaTextColor, FixedCaptionTextColor);
            TrySetDwmWindowColorAttribute(windowHandle, DwmwaBorderColor, FixedCaptionBorderColor);
        }

        /// <summary>
        /// 尝试设置 DWM 窗口颜色属性。
        /// </summary>
        private static void TrySetDwmWindowColorAttribute(IntPtr windowHandle, int attribute, Color color)
        {
            try
            {
                int colorValue = color.R | (color.G << 8) | (color.B << 16);
                DwmSetWindowAttribute(windowHandle, attribute, ref colorValue, sizeof(int));
            }
            catch
            {
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
    }
}
