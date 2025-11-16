
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Backtester2.Converters
{
    public class ValueToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal decimalValue = 0;
            bool isNegative = false;

            if (value is string stringValue)
            {
                if (decimal.TryParse(stringValue.Replace(",", ""), out decimalValue))
                {
                    isNegative = decimalValue < 0;
                }
            }
            else if (value is decimal decValue)
            {
                isNegative = decValue < 0;
            }
            else if (value != null && decimal.TryParse(value.ToString().Replace(",", ""), out decimalValue))
            {
                isNegative = decimalValue < 0;
            }

            if (isNegative)
            {
                // The resource needs to be found from the application's resources
                return App.Current.TryFindResource("Short") as Brush ?? Brushes.Red;
            }
            else
            {
                return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
