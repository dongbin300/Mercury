using System;
using System.Globalization;
using System.Windows.Data;

namespace TradeBot.Converters
{
	public class PercentToBarWidthConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length < 2 || values[0] == null || values[1] == null)
			{
				return 0;
			}

			if (!decimal.TryParse(values[1].ToString(), out decimal barPer))
			{
				return 0;
			}

			var dataGridActualWidth = (double)values[0];
			var widthRatio = Math.Min(Math.Abs((double)barPer), 1);
			return widthRatio * dataGridActualWidth * 0.6;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
