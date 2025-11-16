using System;
using System.Globalization;
using System.Windows.Data;

namespace Backtester2.Converters
{
	public class BarWidthConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length != 2 || values[0] == null || values[1] == null)
				return 0.0;

			if (values[0] is double barRatio && values[1] is double containerWidth)
			{
				// Use ratio (0-1) multiplied by container width with some padding
				var maxBarWidth = Math.Max(0, containerWidth - 10); // 10px padding
				return Math.Max(0, barRatio * maxBarWidth);
			}

			return 0.0;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}