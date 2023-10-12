using System;
using System.Windows.Data;
using System.Windows.Media;

namespace Albedo.Converters
{
    public class StringToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var str = value.ToString();
            if (str == null)
            {
                return new SolidColorBrush(Colors.Black);
            }

            return new SolidColorBrush(Color.FromRgb(
                StringToByte(str.Substring(0, 2)),
                StringToByte(str.Substring(2, 2)),
                StringToByte(str.Substring(4, 2))
                ));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static byte HexCharToByte(char c) => c < 65 ? (byte)(c - 48) : (byte)(c - 55);

        public static byte StringToByte(string str)
        {
            return (byte)(HexCharToByte(str[0]) * 16 + HexCharToByte(str[1]));
        }
    }
}
