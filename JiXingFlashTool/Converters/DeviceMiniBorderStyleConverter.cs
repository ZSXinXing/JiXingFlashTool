using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace JiXingFlashTool.Converters
{
   public class DeviceMiniBorderStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSelected = (bool)value;

            if (isSelected) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D77FC"));
            else return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D1D1"));

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D77FC"));
        }
    }
}
