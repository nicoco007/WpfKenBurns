using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfKenBurns
{
    class FloatToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((float)value * 100).ToString("0") + "%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (float.TryParse(value as string, out var result))
            {
                return result / 100;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
