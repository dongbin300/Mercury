using System;
using System.Windows.Data;
using System.Windows.Media;

namespace MarinerX.Converters
{
    public class StringToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var str = value.ToString() ?? "000000";

            return new SolidColorBrush(Color.FromRgb(
                System.Convert.ToByte(str.Substring(0, 2), 16),
                System.Convert.ToByte(str.Substring(2, 2), 16),
                System.Convert.ToByte(str.Substring(4, 2), 16)
                ));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
