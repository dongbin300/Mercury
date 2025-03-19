using Mercury;

using System;
using System.Windows.Data;
using System.Windows.Media;

namespace TradeBot.Converters
{
	public class CandleColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is Quote quote)
			{
				return quote.Open < quote.Close ? Common.LongColor : quote.Open > quote.Close ? Common.ShortColor : Common.OffColor;
			}

			return Brushes.Transparent;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
