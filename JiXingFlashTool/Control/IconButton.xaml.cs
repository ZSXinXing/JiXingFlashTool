using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace JiXingFlashTool.Control
{
    /// <summary>
    /// 带图标和文字的统一按钮控件，用于承载维护区这类图标+文本操作按钮。
    /// </summary>
    public partial class IconButton : UserControl
    {
        /// <summary>
        /// 初始化图标按钮控件。
        /// </summary>
        public IconButton()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 按钮图标。
        /// </summary>
        public ImageSource ButtonIcon
        {
            get => (ImageSource)GetValue(ButtonIconProperty);
            set => SetValue(ButtonIconProperty, value);
        }

        /// <summary>
        /// ButtonIcon 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonIconProperty =
            DependencyProperty.Register(nameof(ButtonIcon), typeof(ImageSource), typeof(IconButton), new PropertyMetadata(null));

        /// <summary>
        /// 按钮文本。
        /// </summary>
        public string ButtonText
        {
            get => (string)GetValue(ButtonTextProperty);
            set => SetValue(ButtonTextProperty, value);
        }

        /// <summary>
        /// ButtonText 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register(nameof(ButtonText), typeof(string), typeof(IconButton), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 按钮命令。
        /// </summary>
        public ICommand ButtonCommand
        {
            get => (ICommand)GetValue(ButtonCommandProperty);
            set => SetValue(ButtonCommandProperty, value);
        }

        /// <summary>
        /// ButtonCommand 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonCommandProperty =
            DependencyProperty.Register(nameof(ButtonCommand), typeof(ICommand), typeof(IconButton), new PropertyMetadata(null));

        /// <summary>
        /// 按钮命令参数。
        /// </summary>
        public object ButtonCommandParameter
        {
            get => GetValue(ButtonCommandParameterProperty);
            set => SetValue(ButtonCommandParameterProperty, value);
        }

        /// <summary>
        /// ButtonCommandParameter 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonCommandParameterProperty =
            DependencyProperty.Register(nameof(ButtonCommandParameter), typeof(object), typeof(IconButton), new PropertyMetadata(null));

        /// <summary>
        /// 按钮背景色。
        /// </summary>
        public Brush ButtonBackground
        {
            get => (Brush)GetValue(ButtonBackgroundProperty);
            set => SetValue(ButtonBackgroundProperty, value);
        }

        /// <summary>
        /// ButtonBackground 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonBackgroundProperty =
            DependencyProperty.Register(nameof(ButtonBackground), typeof(Brush), typeof(IconButton), new PropertyMetadata(Brushes.Transparent));

        /// <summary>
        /// 按钮边框色。
        /// </summary>
        public Brush ButtonBorderBrush
        {
            get => (Brush)GetValue(ButtonBorderBrushProperty);
            set => SetValue(ButtonBorderBrushProperty, value);
        }

        /// <summary>
        /// ButtonBorderBrush 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonBorderBrushProperty =
            DependencyProperty.Register(nameof(ButtonBorderBrush), typeof(Brush), typeof(IconButton), new PropertyMetadata(Brushes.Transparent));

        /// <summary>
        /// 按钮前景色。
        /// </summary>
        public Brush ButtonForeground
        {
            get => (Brush)GetValue(ButtonForegroundProperty);
            set => SetValue(ButtonForegroundProperty, value);
        }

        /// <summary>
        /// ButtonForeground 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonForegroundProperty =
            DependencyProperty.Register(nameof(ButtonForeground), typeof(Brush), typeof(IconButton), new PropertyMetadata(Brushes.White));

        /// <summary>
        /// 按钮字体大小。
        /// </summary>
        public double ButtonFontSize
        {
            get => (double)GetValue(ButtonFontSizeProperty);
            set => SetValue(ButtonFontSizeProperty, value);
        }

        /// <summary>
        /// ButtonFontSize 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonFontSizeProperty =
            DependencyProperty.Register(nameof(ButtonFontSize), typeof(double), typeof(IconButton), new PropertyMetadata(10.5));

        /// <summary>
        /// 按钮字体粗细。
        /// </summary>
        public FontWeight ButtonFontWeight
        {
            get => (FontWeight)GetValue(ButtonFontWeightProperty);
            set => SetValue(ButtonFontWeightProperty, value);
        }

        /// <summary>
        /// ButtonFontWeight 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonFontWeightProperty =
            DependencyProperty.Register(nameof(ButtonFontWeight), typeof(FontWeight), typeof(IconButton), new PropertyMetadata(FontWeights.Medium));

        /// <summary>
        /// 按钮图标尺寸。
        /// </summary>
        public double ButtonIconSize
        {
            get => (double)GetValue(ButtonIconSizeProperty);
            set => SetValue(ButtonIconSizeProperty, value);
        }

        /// <summary>
        /// ButtonIconSize 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonIconSizeProperty =
            DependencyProperty.Register(nameof(ButtonIconSize), typeof(double), typeof(IconButton), new PropertyMetadata(12.25));

        /// <summary>
        /// 按钮图标和文本之间的间距。
        /// </summary>
        public Thickness ButtonTextMargin
        {
            get => (Thickness)GetValue(ButtonTextMarginProperty);
            set => SetValue(ButtonTextMarginProperty, value);
        }

        /// <summary>
        /// ButtonTextMargin 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ButtonTextMarginProperty =
            DependencyProperty.Register(nameof(ButtonTextMargin), typeof(Thickness), typeof(IconButton), new PropertyMetadata(new Thickness(0)));
    }
}
