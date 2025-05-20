using System.Globalization;
using System.Windows.Data;

namespace CsvPlus
{
	public class QuantityToWidthConverter : IValueConverter
	{
		public decimal MaxQuantity { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var quantity = (int)value;
			if (MaxQuantity == 0)
			{
				return 0;
			}

			return quantity / (double)MaxQuantity * 150;  // 최대 너비 200px
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
