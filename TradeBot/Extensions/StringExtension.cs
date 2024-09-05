using System;
using System.Windows.Media;

namespace TradeBot.Extensions
{
	public static class StringExtension
	{
		public static SolidColorBrush ToSolidColorBrush(this string hexColor)
		{
			if (hexColor.Length != 7 && hexColor.Length != 9)
			{
				return new SolidColorBrush(Colors.White);
			}

			string hex = hexColor.TrimStart('#');
			byte a = 255;

			if (hex.Length == 8)
			{
				a = Convert.ToByte(hex[..2], 16);
				hex = hex[2..];
			}

			byte r = Convert.ToByte(hex[..2], 16);
			byte g = Convert.ToByte(hex.Substring(2, 2), 16);
			byte b = Convert.ToByte(hex.Substring(4, 2), 16);

			Color color = Color.FromArgb(a, r, g, b);

			return new SolidColorBrush(color);
		}
	}
}
