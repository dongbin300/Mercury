using System;
using System.Windows.Media;

namespace TradeBot.Models
{
	public class BotHistory(DateTime dateTime, string subject, string text)
	{
		public DateTime DateTime { get; set; } = dateTime;
		public string Time => DateTime.ToString("yyyy-MM-dd HH:mm:ss");
		public string Text { get; set; } = text;
		public string Subject { get; set; } = subject;
		public SolidColorBrush TextColor => GetTextColor();

		private SolidColorBrush GetTextColor()
		{
			if (Text.StartsWith("Cancel") || Text.Contains("Bot Off") || Text.Contains("Seed Off"))
			{
				return Common.OffColor;
			}
			else if (Text.Contains("Complete") || Text.Contains("Bot On") || Text.Contains("Seed On"))
			{
				return Common.ThemeColor;
			}
			else
			{
				return Common.ForegroundColor;
			}
		}
	}
}
