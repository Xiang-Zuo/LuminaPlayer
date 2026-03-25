using System;
using System.Globalization;
using System.Windows.Data;

namespace LuminaPlayer
{
    /// <summary>
    /// Converts a width to a height using a 16:9 aspect ratio.
    /// </summary>
    public class RatioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && width > 0)
                return width * 9.0 / 16.0;
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
