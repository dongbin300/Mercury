using System;

namespace TradeBot.Models
{
	public class BotReportHistory
	{
		public DateTime Time9 { get; set; }
		public decimal Estimated { get; set; }
		public decimal Bnb { get; set; }
		public decimal TodayPnl { get; set; }
		public decimal BaseOrderSize { get; set; }
		public int Leverage { get; set; }
		public int MaxActiveDeals { get; set; }

		private static readonly char[] separator = ['[', ']'];

		public BotReportHistory(string data)
		{
			var parts = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);
			Time9 = DateTime.Parse(parts[0]);
			var parts2 = parts[1].Split(',');
			Estimated = decimal.Parse(parts2[0].Replace("USDT", "").Trim());
			Bnb = decimal.Parse(parts2[1].Replace("BNB", "").Trim());
			TodayPnl = decimal.Parse(parts2[2].Replace("+", "").Replace("USDT", "").Trim());
			BaseOrderSize = decimal.Parse(parts2[3].Replace("SIZE", "").Trim());
			Leverage = int.Parse(parts2[4].Replace("LEV", "").Trim());
			MaxActiveDeals = int.Parse(parts2[5].Replace("MAX", "").Trim());
		}

		public override string ToString()
		{
			return $"{Time9},{Estimated},{Bnb},{TodayPnl},{BaseOrderSize},{Leverage},{MaxActiveDeals}";
		}
	}
}
