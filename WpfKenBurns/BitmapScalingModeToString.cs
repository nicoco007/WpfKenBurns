using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfKenBurns
{
    public class BitmapScalingModeToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((BitmapScalingMode)value)
            {
                case BitmapScalingMode.Unspecified:
                    return "Default";
                case BitmapScalingMode.NearestNeighbor:
                    return "Low";
                case BitmapScalingMode.LowQuality:
                    return "Medium";
                case BitmapScalingMode.HighQuality:
                    return "High";
            }

            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
