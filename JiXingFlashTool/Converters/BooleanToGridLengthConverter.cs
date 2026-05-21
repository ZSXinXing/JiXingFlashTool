using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JiXingFlashTool.Converters
{
    /// <summary>
    /// 将布尔状态转换为 GridLength，用于按界面状态展开或收起布局行列。
    /// </summary>
    public class BooleanToGridLengthConverter : IValueConverter
    {
        /// <summary>
        /// 根据布尔值返回启用或禁用时的 GridLength。
        /// </summary>
        /// <param name="value">绑定传入的布尔值。</param>
        /// <param name="targetType">目标属性类型。</param>
        /// <param name="parameter">启用高度和禁用高度，格式为 enabled 或 enabled,disabled。</param>
        /// <param name="culture">区域信息。</param>
        /// <returns>转换后的 GridLength。</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var heights = (parameter as string ?? "0").Split(';');
            var enabledHeight = ParseHeight(heights[0]);
            var disabledHeight = heights.Length > 1 ? ParseHeight(heights[1]) : new GridLength(0);

            return value is bool boolValue && boolValue ? enabledHeight : disabledHeight;
        }

        /// <summary>
        /// GridLength 不需要反向转换。
        /// </summary>
        /// <param name="value">目标值。</param>
        /// <param name="targetType">源属性类型。</param>
        /// <param name="parameter">转换参数。</param>
        /// <param name="culture">区域信息。</param>
        /// <returns>始终返回未设置。</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// 将数字字符串解析为像素 GridLength。
        /// </summary>
        /// <param name="heightText">高度文本。</param>
        /// <returns>像素单位的 GridLength。</returns>
        private static GridLength ParseHeight(string heightText)
        {
            return double.TryParse(heightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var height)
                ? new GridLength(height)
                : new GridLength(0);
        }
    }
}
