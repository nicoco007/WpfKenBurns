using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfKenBurns
{
    class FloatToSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((float)value).ToString("0.0") + " s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (float.TryParse(value as string, out var result))
            {
                return result;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
